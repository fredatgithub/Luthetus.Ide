﻿using Luthetus.Common.RazorLib.TreeView.Models.TreeViewClasses;
using Luthetus.Ide.RazorLib.ComponentRenderersCase.Models;
using Luthetus.Ide.RazorLib.TreeViewImplementationsCase.Models;
using Microsoft.AspNetCore.Components;

namespace Luthetus.Ide.RazorLib.TreeViewImplementationsCase.Displays;

public partial class TreeViewAbsolutePathDisplay : ComponentBase,
    ITreeViewAbsolutePathRendererType
{
    [CascadingParameter]
    public TreeViewState TreeViewState { get; set; } = null!;
    [CascadingParameter(Name = "SearchQuery")]
    public string SearchQuery { get; set; } = string.Empty;
    [CascadingParameter(Name = "SearchMatchTuples")]
    public List<(TreeViewStateKey treeViewStateKey, TreeViewAbsolutePath treeViewAbsolutePath)>? SearchMatchTuples { get; set; }

    [Parameter, EditorRequired]
    public TreeViewAbsolutePath TreeViewAbsolutePath { get; set; } = null!;
}