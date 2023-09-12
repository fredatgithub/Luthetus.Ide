﻿using Luthetus.Common.RazorLib.FileSystem.Classes.FilePath;
using Luthetus.Common.RazorLib.Namespaces;
using Luthetus.Common.RazorLib.TreeView.TreeViewClasses;

namespace Luthetus.Ide.ClassLib.TreeViewImplementations.Helper;

public partial class TreeViewHelper
{
    public static async Task<List<TreeViewNoType>> DirectoryLoadChildrenAsync(
        this TreeViewNamespacePath directoryTreeView)
    {
        var directoryAbsoluteFilePathString = directoryTreeView.Item.AbsolutePath
            .FormattedInput;

        var childDirectoryTreeViewModels =
            (await directoryTreeView.FileSystemProvider
                .Directory.GetDirectoriesAsync(directoryAbsoluteFilePathString))
                .OrderBy(filePathString => filePathString)
                .Select(x =>
                {
                    var absoluteFilePath = new AbsolutePath(
                        x,
                        true,
                        directoryTreeView.EnvironmentProvider);

                    var namespaceString = directoryTreeView.Item.Namespace +
                                          NAMESPACE_DELIMITER +
                                          absoluteFilePath.NameNoExtension;

                    var namespacePath = new NamespacePath(
                        namespaceString,
                        absoluteFilePath);

                    return (TreeViewNoType)new TreeViewNamespacePath(
                        namespacePath,
                        directoryTreeView.LuthetusIdeComponentRenderers,
                        directoryTreeView.LuthetusCommonComponentRenderers,
                        directoryTreeView.FileSystemProvider,
                        directoryTreeView.EnvironmentProvider,
                        true,
                        false)
                    {
                        TreeViewChangedKey = TreeViewChangedKey.NewKey()
                    };
                });

        var childFileTreeViewModels =
            (await directoryTreeView.FileSystemProvider
                .Directory.GetFilesAsync(directoryAbsoluteFilePathString))
                .OrderBy(filePathString => filePathString)
                .Select(x =>
                {
                    var absoluteFilePath = new AbsolutePath(
                        x,
                        false,
                        directoryTreeView.EnvironmentProvider);

                    var namespaceString = directoryTreeView.Item.Namespace;

                    var namespacePath = new NamespacePath(
                        namespaceString,
                        absoluteFilePath);

                    return (TreeViewNoType)new TreeViewNamespacePath(
                        namespacePath,
                        directoryTreeView.LuthetusIdeComponentRenderers,
                        directoryTreeView.LuthetusCommonComponentRenderers,
                        directoryTreeView.FileSystemProvider,
                        directoryTreeView.EnvironmentProvider,
                        false,
                        false)
                    {
                        TreeViewChangedKey = TreeViewChangedKey.NewKey()
                    };
                }).ToList();

        var copyOfChildrenToFindRelatedFiles = new List<TreeViewNoType>(childFileTreeViewModels);

        foreach (var child in childFileTreeViewModels)
        {
            child.RemoveRelatedFilesFromParent(
                copyOfChildrenToFindRelatedFiles);
        }

        // The parent directory gets what is left over after the
        // children take their respective 'code behinds'
        childFileTreeViewModels = copyOfChildrenToFindRelatedFiles;

        return childDirectoryTreeViewModels
            .Union(childFileTreeViewModels)
            .ToList();
    }

    public static async Task<List<TreeViewNoType>> LoadChildrenForDirectoryAsync(
        TreeViewAbsolutePath directoryTreeView)
    {
        var directoryAbsoluteFilePathString = directoryTreeView.Item
            .FormattedInput;

        var childDirectoryTreeViewModels =
            (await directoryTreeView.FileSystemProvider
                .Directory.GetDirectoriesAsync(directoryAbsoluteFilePathString))
                .OrderBy(filePathString => filePathString)
                .Select(x =>
                {
                    var absoluteFilePath = new AbsolutePath(
                        x,
                        true,
                        directoryTreeView.EnvironmentProvider);

                    return (TreeViewNoType)new TreeViewAbsoluteFilePath(
                        absoluteFilePath,
                        directoryTreeView.LuthetusIdeComponentRenderers,
                        directoryTreeView.LuthetusCommonComponentRenderers,
                        directoryTreeView.FileSystemProvider,
                        directoryTreeView.EnvironmentProvider,
                        true,
                        false)
                    {
                        TreeViewChangedKey = TreeViewChangedKey.NewKey()
                    };
                });

        var childFileTreeViewModels =
            (await directoryTreeView.FileSystemProvider
                .Directory.GetFilesAsync(directoryAbsoluteFilePathString))
                .OrderBy(filePathString => filePathString)
                .Select(x =>
                {
                    var absoluteFilePath = new AbsolutePath(
                        x,
                        false,
                        directoryTreeView.EnvironmentProvider);

                    return (TreeViewNoType)new TreeViewAbsoluteFilePath(
                        absoluteFilePath,
                        directoryTreeView.LuthetusIdeComponentRenderers,
                        directoryTreeView.LuthetusCommonComponentRenderers,
                        directoryTreeView.FileSystemProvider,
                        directoryTreeView.EnvironmentProvider,
                        false,
                        false)
                    {
                        TreeViewChangedKey = TreeViewChangedKey.NewKey()
                    };
                });

        return childDirectoryTreeViewModels
            .Union(childFileTreeViewModels)
            .ToList();
    }
}