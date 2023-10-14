﻿using Fluxor;
using Luthetus.Common.RazorLib.BackgroundTasks.Models;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.CompilerServices.Lang.CSharp.BinderCase;
using Luthetus.CompilerServices.Lang.CSharp.LexerCase;
using Luthetus.CompilerServices.Lang.CSharp.ParserCase;
using Luthetus.CompilerServices.Lang.CSharp.RuntimeAssemblies;
using Luthetus.TextEditor.RazorLib.Autocompletes.Models;
using Luthetus.TextEditor.RazorLib.CompilerServices;
using Luthetus.TextEditor.RazorLib.Cursors.Models;
using Luthetus.TextEditor.RazorLib.Lexes.Models;
using Luthetus.TextEditor.RazorLib.TextEditors.Models;
using Luthetus.TextEditor.RazorLib.TextEditors.States;
using System.Collections.Immutable;

namespace Luthetus.CompilerServices.Lang.CSharp.CompilerServiceCase;

public class CSharpCompilerService : ICompilerService
{
    private readonly Dictionary<ResourceUri, CSharpResource> _cSharpResourceMap = new();
    private readonly object _cSharpResourceMapLock = new();
    private readonly ITextEditorService _textEditorService;
    private readonly IBackgroundTaskService _backgroundTaskService;
    private readonly IDispatcher _dispatcher;
    
    /// <summary>
    /// TODO: The CSharpBinder should be private, but for now I'm making it public to be usable in the CompilerServiceExplorer Blazor component.
    /// </summary>
    public readonly CSharpBinder Binder = new();

    public CSharpCompilerService(
        ITextEditorService textEditorService,
        IBackgroundTaskService backgroundTaskService,
        IDispatcher dispatcher)
    {
        _textEditorService = textEditorService;
        _backgroundTaskService = backgroundTaskService;
        _dispatcher = dispatcher;

        RuntimeAssembliesLoaderFactory.LoadDotNet6(Binder);
    }

    public ImmutableArray<ICompilerServiceResource> CompilerServiceResources =>
        _cSharpResourceMap.Values
            .Select(csr => (ICompilerServiceResource)csr)
            .ToImmutableArray();

    public event Action? ResourceRegistered;
    public event Action? ResourceParsed;
    public event Action? ResourceDisposed;

    public void RegisterResource(ResourceUri resourceUri)
    {
        lock (_cSharpResourceMapLock)
        {
            if (_cSharpResourceMap.ContainsKey(resourceUri))
                return;

            _cSharpResourceMap.Add(
                resourceUri,
                new(resourceUri, this));
        }

        QueueParseRequest(resourceUri);
        ResourceRegistered?.Invoke();
    }

    public ICompilerServiceResource? GetCompilerServiceResourceFor(ResourceUri resourceUri)
    {
        var model = _textEditorService.Model.FindOrDefaultByResourceUri(resourceUri);

        if (model is null)
            return null;

        lock (_cSharpResourceMapLock)
        {
            if (!_cSharpResourceMap.ContainsKey(resourceUri))
                return null;

            return _cSharpResourceMap[resourceUri];
        }
    }

    public ImmutableArray<TextEditorTextSpan> GetSyntacticTextSpansFor(ResourceUri resourceUri)
    {
        lock (_cSharpResourceMapLock)
        {
            if (!_cSharpResourceMap.ContainsKey(resourceUri))
                return ImmutableArray<TextEditorTextSpan>.Empty;

            return _cSharpResourceMap[resourceUri].SyntacticTextSpans;
        }
    }

    public ImmutableArray<ITextEditorSymbol> GetSymbolsFor(ResourceUri resourceUri)
    {
        lock (_cSharpResourceMapLock)
        {
            if (!_cSharpResourceMap.ContainsKey(resourceUri))
                return ImmutableArray<ITextEditorSymbol>.Empty;

            return _cSharpResourceMap[resourceUri].Symbols;
        }
    }

    public ImmutableArray<TextEditorDiagnostic> GetDiagnosticsFor(ResourceUri resourceUri)
    {
        lock (_cSharpResourceMapLock)
        {
            if (!_cSharpResourceMap.ContainsKey(resourceUri))
                return ImmutableArray<TextEditorDiagnostic>.Empty;

            return _cSharpResourceMap[resourceUri].Diagnostics;
        }
    }

    public void ResourceWasModified(ResourceUri resourceUri, ImmutableArray<TextEditorTextSpan> editTextSpans)
    {
        QueueParseRequest(resourceUri);
    }

    public ImmutableArray<AutocompleteEntry> GetAutocompleteEntries(string word, TextEditorCursorSnapshot cursorSnapshot)
    {
        if (Binder.TryGetTypeHierarchically(word, out var typeDefinitionNode) &&
            typeDefinitionNode is not null)
        {
            var functionDefinitionNodes = typeDefinitionNode.GetFunctionDefinitionNodes();

            var matches = functionDefinitionNodes
                .Where(fdn => fdn.FunctionIdentifier.TextSpan.GetText().Contains(word))
                .Select(fdn =>
                {
                    return new AutocompleteEntry(
                        fdn.FunctionIdentifier.TextSpan.GetText(),
                        AutocompleteEntryKind.Function);
                });

            return matches.ToImmutableArray();
        }

        return ImmutableArray<AutocompleteEntry>.Empty;
    }

    public void DisposeResource(ResourceUri resourceUri)
    {
        lock (_cSharpResourceMapLock)
        {
            _cSharpResourceMap.Remove(resourceUri);
        }

        ResourceDisposed?.Invoke();
    }

    private void QueueParseRequest(ResourceUri resourceUri)
    {
        _backgroundTaskService.Enqueue(Key<BackgroundTask>.NewKey(), ContinuousBackgroundTaskWorker.Queue.Key,
            "C# Compiler Service - Parse",
            async () =>
            {
                var model = _textEditorService.Model.FindOrDefaultByResourceUri(resourceUri);

                if (model is null)
                    return;

                _dispatcher.Dispatch(new TextEditorModelState.CalculatePresentationModelAction(
                    model.ResourceUri,
                    CompilerServiceDiagnosticPresentationFacts.PresentationKey));

                var pendingCalculation = model.PresentationModelsBag.FirstOrDefault(x =>
                    x.TextEditorPresentationKey == CompilerServiceDiagnosticPresentationFacts.PresentationKey)
                    ?.PendingCalculation;

                if (pendingCalculation is null)
                    pendingCalculation = new(model.GetAllText());

                var lexer = new CSharpLexer(resourceUri, pendingCalculation.ContentAtRequest);
                lexer.Lex();

                var parser = new CSharpParser(lexer);
                var compilationUnit = parser.Parse(Binder, resourceUri);

                if (compilationUnit is null)
                    return;

                lock (_cSharpResourceMapLock)
                {
                    if (!_cSharpResourceMap.ContainsKey(resourceUri))
                        return;

                    var cSharpResource = _cSharpResourceMap[resourceUri];

                    cSharpResource.CompilationUnit = compilationUnit;
                    cSharpResource.SyntaxTokens = lexer.SyntaxTokens;
                }

                // TODO: Shouldn't one get a reference to the most recent TextEditorModel instance with the given key and invoke .ApplySyntaxHighlightingAsync() on that?
                await model.ApplySyntaxHighlightingAsync();

                ResourceParsed?.Invoke();

                var presentationModel = model.PresentationModelsBag.FirstOrDefault(x =>
                    x.TextEditorPresentationKey == CompilerServiceDiagnosticPresentationFacts.PresentationKey);

                if (presentationModel is not null)
                {
                    presentationModel.PendingCalculation.TextEditorTextSpanBag =
                        GetDiagnosticsFor(model.ResourceUri)
                            .Select(x => x.TextSpan)
                            .ToImmutableArray();

                    (presentationModel.CompletedCalculation, presentationModel.PendingCalculation) =
                        (presentationModel.PendingCalculation, presentationModel.CompletedCalculation);
                }

                return;
            });
    }
}