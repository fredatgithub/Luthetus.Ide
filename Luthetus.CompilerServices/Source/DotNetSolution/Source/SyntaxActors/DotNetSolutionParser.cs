﻿using System.Collections.Immutable;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.SyntaxTokens;
using Luthetus.TextEditor.RazorLib.CompilerServices;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax;
using Luthetus.CompilerServices.Lang.DotNetSolution.Facts;
using Luthetus.TextEditor.RazorLib.Lexes.Models;
using Luthetus.CompilerServices.Lang.DotNetSolution.Models.Associated;
using Luthetus.CompilerServices.Lang.DotNetSolution.Models;
using Luthetus.CompilerServices.Lang.DotNetSolution.Models.Project;

namespace Luthetus.CompilerServices.Lang.DotNetSolution.SyntaxActors;

public class DotNetSolutionParser : IParser
{
    private readonly TokenWalker _tokenWalker;
    private readonly LuthetusDiagnosticBag _diagnosticBag = new();
    private readonly Stack<AssociatedEntryGroupBuilder> _associatedEntryGroupBuilderStack = new();

    private DotNetSolutionHeader _dotNetSolutionHeader = new();
    private bool _hasReadHeader;
    private DotNetSolutionGlobal _dotNetSolutionGlobal = new();
    private AssociatedEntryGroup? _noParentHavingAssociatedEntryGroup;
    private List<IDotNetProject> _dotNetProjectBag = new();
    private List<NestedProjectEntry> _nestedProjectEntryBag = new();

    public DotNetSolutionParser(DotNetSolutionLexer lexer)
    {
        Lexer = lexer;
        _tokenWalker = new TokenWalker(lexer.SyntaxTokens, _diagnosticBag);
    }

    public ImmutableArray<TextEditorDiagnostic> DiagnosticsBag => _diagnosticBag.ToImmutableArray();
    public DotNetSolutionLexer Lexer { get; }

    public DotNetSolutionHeader DotNetSolutionHeader => _dotNetSolutionHeader;
    public DotNetSolutionGlobal DotNetSolutionGlobal => _dotNetSolutionGlobal;
    public AssociatedEntryGroup? NoParentHavingAssociatedEntryGroup => _noParentHavingAssociatedEntryGroup;
    public List<IDotNetProject> DotNetProjectBag => _dotNetProjectBag;
    public List<NestedProjectEntry> NestedProjectEntryBag => _nestedProjectEntryBag;

    public CompilationUnit Parse()
    {
        while (true)
        {
            var consumedToken = _tokenWalker.Consume();

            switch (consumedToken.SyntaxKind)
            {
                case SyntaxKind.AssociatedNameToken:
                    ParseAssociatedNameToken((AssociatedNameToken)consumedToken);
                    break;
                case SyntaxKind.AssociatedValueToken:
                    ParseAssociatedValueToken((AssociatedValueToken)consumedToken);
                    break;
                case SyntaxKind.OpenAssociatedGroupToken:
                    ParseOpenAssociatedGroupToken((OpenAssociatedGroupToken)consumedToken);
                    break;
                case SyntaxKind.CloseAssociatedGroupToken:
                    ParseCloseAssociatedGroupToken((CloseAssociatedGroupToken)consumedToken);
                    break;
                default:
                    break;
            }

            if (consumedToken.SyntaxKind == SyntaxKind.EndOfFileToken)
                break;
        }

        var globalSectionNestedProjects = DotNetSolutionGlobal.DotNetSolutionGlobalSectionBag.FirstOrDefault(x =>
        {
            return (x.GlobalSectionArgument?.TextSpan.GetText() ?? string.Empty) == 
                LexSolutionFacts.GlobalSectionNestedProjects.START_TOKEN;
        });

        if (globalSectionNestedProjects is not null)
        {
            foreach (var associatedEntry in globalSectionNestedProjects.AssociatedEntryGroup.AssociatedEntryBag)
            {
                switch (associatedEntry.AssociatedEntryKind)
                {
                    case AssociatedEntryKind.Pair:
                        var pair = (AssociatedEntryPair)associatedEntry;

                        if (Guid.TryParse(pair.AssociatedNameToken.TextSpan.GetText(),
                                out var childProjectIdGuid))
                        {
                            if (Guid.TryParse(pair.AssociatedValueToken.TextSpan.GetText(),
                                    out var solutionFolderIdGuid))
                            {
                                var nestedProjectEntry = new NestedProjectEntry(
                                    childProjectIdGuid,
                                    solutionFolderIdGuid);

                                _nestedProjectEntryBag.Add(nestedProjectEntry);
                            }
                        }

                        break;
                    default:
                        break;
                }
            }
        }

        return new CompilationUnit(
            null,
            Lexer,
            this,
            null);
    }

    public void ParseAssociatedNameToken(AssociatedNameToken associatedNameToken)
    {
        if (!_hasReadHeader)
        {
            foreach (var wellKnownAssociatedName in LexSolutionFacts.Header.WellKnownAssociatedNamesBag)
            {
                if (associatedNameToken.TextSpan.GetText() == wellKnownAssociatedName)
                {
                    var associatedValueToken = (AssociatedValueToken)_tokenWalker.Match(SyntaxKind.AssociatedValueToken);

                    switch (wellKnownAssociatedName)
                    {
                        case LexSolutionFacts.Header.EXACT_VISUAL_STUDIO_VERSION_START_TOKEN:
                            _dotNetSolutionHeader = _dotNetSolutionHeader with
                            {
                                ExactVisualStudioVersionPair = new AssociatedEntryPair(
                                    associatedNameToken, associatedValueToken),
                            };
                            break;
                        case LexSolutionFacts.Header.FORMAT_VERSION_START_TOKEN:
                            _dotNetSolutionHeader = _dotNetSolutionHeader with
                            {
                                FormatVersionPair = new AssociatedEntryPair(
                                    associatedNameToken, associatedValueToken),
                            };
                            break;
                        case LexSolutionFacts.Header.HASHTAG_VISUAL_STUDIO_VERSION_START_TOKEN:
                            _dotNetSolutionHeader = _dotNetSolutionHeader with
                            {
                                HashtagVisualStudioVersionPair = new AssociatedEntryPair(
                                    associatedNameToken, associatedValueToken),
                            };
                            break;
                        case LexSolutionFacts.Header.MINIMUM_VISUAL_STUDIO_VERSION_START_TOKEN:
                            _dotNetSolutionHeader = _dotNetSolutionHeader with
                            {
                                MinimumVisualStudioVersionPair = new AssociatedEntryPair(
                                    associatedNameToken, associatedValueToken),
                            };
                            break;
                    }
                }
            }
        }
        else
        {
            var associatedValueToken = (AssociatedValueToken)_tokenWalker.Match(SyntaxKind.AssociatedValueToken);
            var associatedEntryPair = new AssociatedEntryPair(associatedNameToken, associatedValueToken);

            _associatedEntryGroupBuilderStack.Peek().AssociatedEntryBag.Add(associatedEntryPair);
        }
    }

    public void ParseAssociatedValueToken(AssociatedValueToken associatedValueToken)
    {
        // One enters this method when parsing the Project definitions.
        // They are one value after another, no names involved.

        var associatedEntryPair = new AssociatedEntryPair(
            new AssociatedNameToken(TextEditorTextSpan.FabricateTextSpan(string.Empty)),
            associatedValueToken);

        _associatedEntryGroupBuilderStack.Peek().AssociatedEntryBag.Add(associatedEntryPair);
    }

    private void ParseOpenAssociatedGroupToken(OpenAssociatedGroupToken openAssociatedGroupToken)
    {
        // Presumption is made here, that the header only contains AssociatedEntryPairs
        _hasReadHeader = true;

        var success = _associatedEntryGroupBuilderStack.TryPeek(out var parent);

        if (openAssociatedGroupToken.TextSpan.GetText() == LexSolutionFacts.Global.START_TOKEN)
        {
            _dotNetSolutionGlobal = _dotNetSolutionGlobal with
            {
                WasFound = true,
                OpenAssociatedGroupToken = openAssociatedGroupToken
            };
        }
        else if (openAssociatedGroupToken.TextSpan.GetText() == LexSolutionFacts.GlobalSection.START_TOKEN)
        {
            // TODO: Should this be re-written without using a closure hack?
            var localDotNetSolutionGlobalSectionBuilder = new DotNetSolutionGlobalSectionBuilder();

            _associatedEntryGroupBuilderStack.Push(new AssociatedEntryGroupBuilder(
                openAssociatedGroupToken,
                builtGroup =>
                {
                    localDotNetSolutionGlobalSectionBuilder.AssociatedEntryGroup = builtGroup;

                    var outBag = _dotNetSolutionGlobal.DotNetSolutionGlobalSectionBag.Add(
                        localDotNetSolutionGlobalSectionBuilder.Build());

                    _dotNetSolutionGlobal = _dotNetSolutionGlobal with
                    {
                        DotNetSolutionGlobalSectionBag = outBag
                    };
                }));

            localDotNetSolutionGlobalSectionBuilder.GlobalSectionArgument =
                (AssociatedValueToken)_tokenWalker.Match(SyntaxKind.AssociatedValueToken);

            localDotNetSolutionGlobalSectionBuilder.GlobalSectionOrder =
                (AssociatedValueToken)_tokenWalker.Match(SyntaxKind.AssociatedValueToken);
        }
        else if (openAssociatedGroupToken.TextSpan.GetText() == LexSolutionFacts.Project.PROJECT_DEFINITION_START_TOKEN)
        {
            _associatedEntryGroupBuilderStack.Push(new AssociatedEntryGroupBuilder(
                openAssociatedGroupToken,
                builtGroup =>
                {
                    if (builtGroup.AssociatedEntryBag.Length == 4)
                    {
                        var i = 0;

                        var projectTypeGuidAssociatedPair = builtGroup.AssociatedEntryBag[i++] as AssociatedEntryPair;
                        var displayNameAssociatedPair = builtGroup.AssociatedEntryBag[i++] as AssociatedEntryPair;
                        var relativePathFromSolutionFileStringAssociatedPair = builtGroup.AssociatedEntryBag[i++] as AssociatedEntryPair;
                        var projectIdGuidAssociatedPair = builtGroup.AssociatedEntryBag[i++] as AssociatedEntryPair;

                        _ = Guid.TryParse(projectTypeGuidAssociatedPair.AssociatedValueToken.TextSpan.GetText(), out var projectTypeGuid);
                        var displayName = displayNameAssociatedPair.AssociatedValueToken.TextSpan.GetText();
                        var relativePathFromSolutionFileString = relativePathFromSolutionFileStringAssociatedPair.AssociatedValueToken.TextSpan.GetText();
                        _ = Guid.TryParse(projectIdGuidAssociatedPair.AssociatedValueToken.TextSpan.GetText(), out var projectIdGuid);

                        IDotNetProject dotNetProject;

                        if (projectTypeGuid == SolutionFolder.SolutionFolderProjectTypeGuid)
                        {
                            dotNetProject = new SolutionFolder(
                                displayName,
                                projectTypeGuid,
                                relativePathFromSolutionFileString,
                                projectIdGuid,
                                builtGroup.OpenAssociatedGroupToken,
                                builtGroup.CloseAssociatedGroupToken,
                                null);
                        }
                        else
                        {
                            dotNetProject = new CSharpProject(
                                displayName,
                                projectTypeGuid,
                                relativePathFromSolutionFileString,
                                projectIdGuid,
                                builtGroup.OpenAssociatedGroupToken,
                                builtGroup.CloseAssociatedGroupToken,
                                null);
                        }

                        _dotNetProjectBag.Add(dotNetProject);
                    }

                    _noParentHavingAssociatedEntryGroup = builtGroup;
                }));
        }
        else if (parent is not null)
        {
            _associatedEntryGroupBuilderStack.Push(new AssociatedEntryGroupBuilder(
                openAssociatedGroupToken,
                builtGroup =>
                {
                    parent.AssociatedEntryBag.Add(builtGroup);
                }));
        }
    }

    private void ParseCloseAssociatedGroupToken(CloseAssociatedGroupToken closeAssociatedGroupToken)
    {
        if (closeAssociatedGroupToken.TextSpan.GetText() == LexSolutionFacts.Global.END_TOKEN)
        {
            _dotNetSolutionGlobal = _dotNetSolutionGlobal with
            {
                CloseAssociatedGroupToken = closeAssociatedGroupToken
            };
        }
        else if (closeAssociatedGroupToken.TextSpan.GetText() == LexSolutionFacts.GlobalSection.END_TOKEN)
        {
            var associatedEntryGroupBuilder = _associatedEntryGroupBuilderStack.Pop();
            associatedEntryGroupBuilder.CloseAssociatedGroupToken = closeAssociatedGroupToken;

            associatedEntryGroupBuilder.Build();
        }
        else
        {
            var associatedEntryGroupBuilder = _associatedEntryGroupBuilderStack.Pop();
            associatedEntryGroupBuilder.CloseAssociatedGroupToken = closeAssociatedGroupToken;

            associatedEntryGroupBuilder.Build();
        }
    }
}