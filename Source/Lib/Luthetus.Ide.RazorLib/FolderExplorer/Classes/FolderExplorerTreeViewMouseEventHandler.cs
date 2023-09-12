﻿using Fluxor;
using Luthetus.Common.RazorLib.TreeView;
using Luthetus.Common.RazorLib.TreeView.Commands;
using Luthetus.Common.RazorLib.TreeView.Events;
using Luthetus.Ide.ClassLib.Store.EditorCase;
using Luthetus.Ide.ClassLib.TreeViewImplementations;

namespace Luthetus.Ide.RazorLib.FolderExplorer.Classes;

public class FolderExplorerTreeViewMouseEventHandler : TreeViewMouseEventHandler
{
    private readonly IDispatcher _dispatcher;

    public FolderExplorerTreeViewMouseEventHandler(
        IDispatcher dispatcher,
        ITreeViewService treeViewService)
        : base(treeViewService)
    {
        _dispatcher = dispatcher;
    }

    public override void OnDoubleClick(ITreeViewCommandParameter treeViewCommandParameter)
    {
        base.OnDoubleClick(treeViewCommandParameter);

        if (treeViewCommandParameter.TargetNode is not TreeViewAbsolutePath treeViewAbsoluteFilePath)
            return;

        _dispatcher.Dispatch(new EditorRegistry.OpenInEditorAction(
            treeViewAbsoluteFilePath.Item,
            true));
    }
}