﻿using Luthetus.Ide.ClassLib.CompilerServices.Common.Syntax;
using Luthetus.TextEditor.RazorLib.Analysis;
using System.Collections.Immutable;

namespace Luthetus.Ide.ClassLib.CompilerServices.Common.General;

/// <summary>
/// The <see cref="CompilationUnit"/> is used to represent
/// a singular C# resource file (that is to say a singular file on the user's file system).
/// <br/><br/>
/// TODO: How should <see cref="CompilationUnit"/> work in regards to the C# 'partial' keyword, would many C# resource files need be stitched together into a single <see cref="CompilationUnit"/>?
/// </summary>
public sealed record CompilationUnit : ISyntaxNode
{
    public CompilationUnit(
        bool isExpression,
        ImmutableArray<ISyntax> children)
    {
        IsExpression = isExpression;
        Children = children;

        Diagnostics = ImmutableArray<TextEditorDiagnostic>.Empty;
    }

    public CompilationUnit(
        bool isExpression,
        ImmutableArray<ISyntax> children,
        ImmutableArray<TextEditorDiagnostic> diagnostics)
    {
        IsExpression = isExpression;
        Children = children;
        Diagnostics = diagnostics;
    }

    public bool IsExpression { get; init; }
    public ImmutableArray<TextEditorDiagnostic> Diagnostics { get; init; }

    public ImmutableArray<ISyntax> Children { get; init; }
    public bool IsFabricated { get; init; }
    public SyntaxKind SyntaxKind => SyntaxKind.CompilationUnitNode;
}
