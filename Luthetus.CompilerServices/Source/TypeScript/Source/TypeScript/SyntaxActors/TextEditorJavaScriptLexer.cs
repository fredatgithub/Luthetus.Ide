﻿using System.Collections.Immutable;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.Common.RazorLib.RenderStates.Models;
using Luthetus.CompilerServices.Lang.TypeScript.TypeScript.Facts;
using Luthetus.TextEditor.RazorLib.CompilerServices;
using Luthetus.TextEditor.RazorLib.CompilerServices.GenericLexer;
using Luthetus.TextEditor.RazorLib.CompilerServices.GenericLexer.SyntaxActors;
using Luthetus.TextEditor.RazorLib.Lexes.Models;

namespace Luthetus.CompilerServices.Lang.TypeScript.TypeScript.SyntaxActors;

public class TextEditorTypeScriptLexer
{
    public static readonly GenericPreprocessorDefinition TypeScriptPreprocessorDefinition = new(
        "#",
        ImmutableArray<DeliminationExtendedSyntaxDefinition>.Empty);

    public static readonly GenericLanguageDefinition TypeScriptLanguageDefinition = new GenericLanguageDefinition(
        "\"",
        "\"",
        "(",
        ")",
        ".",
        "//",
        new[]
        {
        WhitespaceFacts.CARRIAGE_RETURN.ToString(),
        WhitespaceFacts.LINE_FEED.ToString()
        }.ToImmutableArray(),
        "/*",
        "*/",
        TypeScriptKeywords.ALL,
        TypeScriptPreprocessorDefinition);

    private readonly GenericSyntaxTree _typeScriptSyntaxTree;

    public TextEditorTypeScriptLexer(ResourceUri resourceUri)
    {
        _typeScriptSyntaxTree = new GenericSyntaxTree(TypeScriptLanguageDefinition);
        ResourceUri = resourceUri;
    }

    public Key<RenderState> ModelRenderStateKey { get; private set; } = Key<RenderState>.Empty;

    public ResourceUri ResourceUri { get; }

    public Task<ImmutableArray<TextEditorTextSpan>> Lex(
        string sourceText,
        Key<RenderState> modelRenderStateKey)
    {
        var typeScriptSyntaxUnit = _typeScriptSyntaxTree.ParseText(
            ResourceUri,
            sourceText);

        var typeScriptSyntaxWalker = new GenericSyntaxWalker();

        typeScriptSyntaxWalker.Visit(typeScriptSyntaxUnit.GenericDocumentSyntax);

        var textEditorTextSpans = new List<TextEditorTextSpan>();

        textEditorTextSpans
            .AddRange(typeScriptSyntaxWalker.StringSyntaxBag
                .Select(x => x.TextSpan));

        textEditorTextSpans
            .AddRange(typeScriptSyntaxWalker.CommentSingleLineSyntaxBag
                .Select(x => x.TextSpan));

        textEditorTextSpans
            .AddRange(typeScriptSyntaxWalker.CommentMultiLineSyntaxBag
                .Select(x => x.TextSpan));

        textEditorTextSpans
            .AddRange(typeScriptSyntaxWalker.KeywordSyntaxBag
                .Select(x => x.TextSpan));

        textEditorTextSpans
            .AddRange(typeScriptSyntaxWalker.FunctionSyntaxBag
                .Select(x => x.TextSpan));

        return Task.FromResult(textEditorTextSpans.ToImmutableArray());
    }
}