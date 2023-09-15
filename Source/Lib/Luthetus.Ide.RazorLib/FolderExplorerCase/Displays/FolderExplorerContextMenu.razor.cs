﻿using Fluxor;
using Luthetus.Common.RazorLib.Dimensions;
using Microsoft.AspNetCore.Components;
using System.Collections.Immutable;
using Luthetus.Ide.RazorLib.MenuCase.Models;
using Luthetus.Ide.RazorLib.TreeViewImplementationsCase.Models;
using Luthetus.Ide.RazorLib.FolderExplorerCase.States;
using Luthetus.Common.RazorLib.ComponentRenderers.Models;
using Luthetus.Common.RazorLib.Dropdown.Models;
using Luthetus.Common.RazorLib.Menu.Models;
using Luthetus.Common.RazorLib.Notification.Models;
using Luthetus.Common.RazorLib.TreeView.Models;
using Luthetus.Common.RazorLib.TreeView.Models.TreeViewClasses;

namespace Luthetus.Ide.RazorLib.FolderExplorerCase.Displays;

public partial class FolderExplorerContextMenu : ComponentBase
{
    [Inject]
    private IDispatcher Dispatcher { get; set; } = null!;
    [Inject]
    private IMenuOptionsFactory MenuOptionsFactory { get; set; } = null!;
    [Inject]
    private ILuthetusCommonComponentRenderers LuthetusCommonComponentRenderers { get; set; } = null!;
    [Inject]
    private ITreeViewService TreeViewService { get; set; } = null!;

    [Parameter, EditorRequired]
    public ITreeViewCommandParameter TreeViewCommandParameter { get; set; } = null!;

    public static readonly DropdownKey ContextMenuEventDropdownKey = DropdownKey.NewKey();

    /// <summary>
    /// The program is currently running using Photino locally on the user's computer
    /// therefore this static solution works without leaking any information.
    /// </summary>
    public static TreeViewNoType? ParentOfCutFile;

    private MenuRecord GetMenuRecord(
        ITreeViewCommandParameter treeViewCommandParameter)
    {
        if (treeViewCommandParameter.TargetNode is null)
            return MenuRecord.Empty;

        var menuRecords = new List<MenuOptionRecord>();

        var treeViewModel = treeViewCommandParameter.TargetNode;
        var parentTreeViewModel = treeViewModel.Parent;

        var parentTreeViewAbsolutePath = parentTreeViewModel as TreeViewAbsolutePath;

        if (treeViewModel is not TreeViewAbsolutePath treeViewAbsolutePath)
            return MenuRecord.Empty;

        if (treeViewAbsolutePath.Item.IsDirectory)
        {
            menuRecords.AddRange(
                GetFileMenuOptions(treeViewAbsolutePath, parentTreeViewAbsolutePath)
                    .Union(GetDirectoryMenuOptions(treeViewAbsolutePath))
                    .Union(GetDebugMenuOptions(treeViewAbsolutePath)));
        }
        else
        {
            menuRecords.AddRange(
                GetFileMenuOptions(treeViewAbsolutePath, parentTreeViewAbsolutePath)
                    .Union(GetDebugMenuOptions(treeViewAbsolutePath)));
        }

        return new MenuRecord(
            menuRecords.ToImmutableArray());
    }

    private MenuOptionRecord[] GetDirectoryMenuOptions(TreeViewAbsolutePath treeViewModel)
    {
        return new[]
        {
        MenuOptionsFactory.NewEmptyFile(
            treeViewModel.Item,
            async () => await ReloadTreeViewModel(treeViewModel)),
        MenuOptionsFactory.NewDirectory(
            treeViewModel.Item,
            async () => await ReloadTreeViewModel(treeViewModel)),
        MenuOptionsFactory.PasteClipboard(
            treeViewModel.Item,
            async () =>
            {
                var localParentOfCutFile =
                    ParentOfCutFile;

                ParentOfCutFile = null;

                if (localParentOfCutFile is not null)
                    await ReloadTreeViewModel(localParentOfCutFile);

                await ReloadTreeViewModel(treeViewModel);
            }),
    };
    }

    private MenuOptionRecord[] GetFileMenuOptions(
        TreeViewAbsolutePath treeViewModel,
        TreeViewAbsolutePath? parentTreeViewModel)
    {
        return new[]
        {
            MenuOptionsFactory.CopyFile(treeViewModel.Item, () => {
                NotificationHelper.DispatchInformative("Copy Action", $"Copied: {treeViewModel.Item.NameWithExtension}", LuthetusCommonComponentRenderers, Dispatcher);
                return Task.CompletedTask;
            }),
            MenuOptionsFactory.CutFile(treeViewModel.Item, () => {
                NotificationHelper.DispatchInformative("Cut Action", $"Cut: {treeViewModel.Item.NameWithExtension}", LuthetusCommonComponentRenderers, Dispatcher);
                ParentOfCutFile = parentTreeViewModel;
                return Task.CompletedTask;
            }),
            MenuOptionsFactory.DeleteFile(treeViewModel.Item, async () => await ReloadTreeViewModel(parentTreeViewModel)),
            MenuOptionsFactory.RenameFile(treeViewModel.Item, Dispatcher, async ()  => await ReloadTreeViewModel(parentTreeViewModel))
        };
    }

    private MenuOptionRecord[] GetDebugMenuOptions(
        TreeViewAbsolutePath treeViewModel)
    {
        return new MenuOptionRecord[]
        {
            // new MenuOptionRecord(
            //     $"namespace: {treeViewModel.Item.Namespace}",
            //     MenuOptionKind.Read)
        };
    }

    /// <summary>
    /// This method I believe is causing bugs
    /// <br/><br/>
    /// For example, when removing a C# Project the
    /// solution is reloaded and a new root is made.
    /// <br/><br/>
    /// Then there is a timing issue where the new root is made and set
    /// as the root. But this method erroneously reloads the old root.
    /// </summary>
    /// <param name="treeViewModel"></param>
    private async Task ReloadTreeViewModel(
        TreeViewNoType? treeViewModel)
    {
        if (treeViewModel is null)
            return;

        await treeViewModel.LoadChildrenAsync();

        TreeViewService.ReRenderNode(
            FolderExplorerState.TreeViewFolderExplorerContentStateKey,
            treeViewModel);

        TreeViewService.MoveUp(
            FolderExplorerState.TreeViewFolderExplorerContentStateKey,
            false);
    }

    public static string GetContextMenuCssStyleString(ITreeViewCommandParameter? treeViewCommandParameter)
    {
        if (treeViewCommandParameter?.ContextMenuFixedPosition is null)
            return "display: none;";

        var left =
            $"left: {treeViewCommandParameter.ContextMenuFixedPosition.LeftPositionInPixels.ToCssValue()}px;";

        var top =
            $"top: {treeViewCommandParameter.ContextMenuFixedPosition.TopPositionInPixels.ToCssValue()}px;";

        return $"{left} {top}";
    }
}