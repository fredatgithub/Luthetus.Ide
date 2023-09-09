﻿using Luthetus.CompilerServices.Lang.DotNetSolution.CSharp;
using Luthetus.Ide.ClassLib.ComponentRenderers.Types.TreeViews;
using Microsoft.AspNetCore.Components;

namespace Luthetus.Ide.RazorLib.TreeViewImplementations;

public partial class TreeViewCSharpProjectToProjectReferenceDisplay :
    ComponentBase, ITreeViewCSharpProjectToProjectReferenceRendererType
{
    [Parameter, EditorRequired]
    public CSharpProjectToProjectReference CSharpProjectToProjectReference { get; set; } = null!;
}