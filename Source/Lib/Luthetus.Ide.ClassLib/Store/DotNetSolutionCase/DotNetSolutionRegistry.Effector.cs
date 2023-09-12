﻿using Luthetus.Ide.ClassLib.ComponentRenderers;
using Luthetus.Ide.ClassLib.TreeViewImplementations;
using Luthetus.Common.RazorLib.TreeView;
using Fluxor;
using Luthetus.Common.RazorLib.TreeView.TreeViewClasses;
using System.Collections.Immutable;
using Luthetus.Common.RazorLib.FileSystem.Interfaces;
using Luthetus.Common.RazorLib.Namespaces;
using Luthetus.CompilerServices.Lang.DotNetSolution;
using Luthetus.Common.RazorLib.ComponentRenderers;
using Luthetus.Common.RazorLib.FileSystem.Classes.LuthetusPath;
using Luthetus.Common.RazorLib.BackgroundTaskCase.BaseTypes;

namespace Luthetus.Ide.ClassLib.Store.DotNetSolutionCase;

public partial record DotNetSolutionRegistry
{
    private class Effector
    {
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IEnvironmentProvider _environmentProvider;
        private readonly ILuthetusIdeComponentRenderers _luthetusIdeComponentRenderers;
        private readonly ILuthetusCommonComponentRenderers _luthetusCommonComponentRenderers;
        private readonly ITreeViewService _treeViewService;
        private readonly IState<DotNetSolutionRegistry> _dotNetSolutionStateWrap;
        private readonly IBackgroundTaskService _backgroundTaskService;

        public Effector(
            IFileSystemProvider fileSystemProvider,
            IEnvironmentProvider environmentProvider,
            ILuthetusIdeComponentRenderers luthetusIdeComponentRenderers,
            ILuthetusCommonComponentRenderers luthetusCommonComponentRenderers,
            ITreeViewService treeViewService,
            IState<DotNetSolutionRegistry> dotNetSolutionStateWrap,
            IBackgroundTaskService backgroundTaskService)
        {
            _fileSystemProvider = fileSystemProvider;
            _environmentProvider = environmentProvider;
            _luthetusIdeComponentRenderers = luthetusIdeComponentRenderers;
            _luthetusCommonComponentRenderers = luthetusCommonComponentRenderers;
            _treeViewService = treeViewService;
            _dotNetSolutionStateWrap = dotNetSolutionStateWrap;
            _backgroundTaskService = backgroundTaskService;
        }

        [EffectMethod]
        public async Task HandleSetDotNetSolutionAction(
            SetDotNetSolutionAction setDotNetSolutionAction,
            IDispatcher dispatcher)
        {
            var dotNetSolutionAbsolutePathString = setDotNetSolutionAction.SolutionAbsolutePath.FormattedInput;

            var content = await _fileSystemProvider.File.ReadAllTextAsync(
                dotNetSolutionAbsolutePathString,
                CancellationToken.None);

            var solutionAbsolutePath = new AbsolutePath(
                dotNetSolutionAbsolutePathString,
                false,
                _environmentProvider);

            var solutionNamespacePath = new NamespacePath(
                string.Empty,
                solutionAbsolutePath);

            var dotNetSolution = DotNetSolutionLexer.Lex(
                content,
                solutionNamespacePath,
                _environmentProvider);

            dispatcher.Dispatch(new RegisterAction(dotNetSolution));
            
            dispatcher.Dispatch(new WithAction(
                inDotNetSolutionState => inDotNetSolutionState with
                {
                    DotNetSolutionModelKey = dotNetSolution.DotNetSolutionModelKey
                }));

            dispatcher.Dispatch(new SetDotNetSolutionTreeViewAction());
            dispatcher.Dispatch(new ParseDotNetSolutionAction());
        }

        [EffectMethod(typeof(SetDotNetSolutionTreeViewAction))]
        public async Task HandleSetDotNetSolutionTreeViewAction(
            IDispatcher dispatcher)
        {
            var dotNetSolutionState = _dotNetSolutionStateWrap.Value;

            var dotNetSolutionModel = dotNetSolutionState.DotNetSolutions.FirstOrDefault(x =>
                x.DotNetSolutionModelKey == dotNetSolutionState.DotNetSolutionModelKey);

            if (dotNetSolutionModel is null)
                return;

            dispatcher.Dispatch(new WithAction(inDotNetSolutionState =>
                inDotNetSolutionState with
                {
                    IsExecutingAsyncTaskLinks = inDotNetSolutionState.IsExecutingAsyncTaskLinks + 1
                }));

            var rootNode = new TreeViewSolution(
                dotNetSolutionModel,
                _luthetusIdeComponentRenderers,
                _luthetusCommonComponentRenderers,
                _fileSystemProvider,
                _environmentProvider,
                true,
                true);

            await rootNode.LoadChildrenAsync();

            if (!_treeViewService.TryGetTreeViewState(
                    TreeViewSolutionExplorerStateKey,
                    out _))
            {
                _treeViewService.RegisterTreeViewState(new TreeViewState(
                    TreeViewSolutionExplorerStateKey,
                    rootNode,
                    rootNode,
                    ImmutableList<TreeViewNoType>.Empty));
            }
            else
            {
                _treeViewService.SetRoot(
                    TreeViewSolutionExplorerStateKey,
                    rootNode);

                _treeViewService.SetActiveNode(
                    TreeViewSolutionExplorerStateKey,
                    rootNode);
            }

            dispatcher.Dispatch(new WithAction(inDotNetSolutionState =>
                inDotNetSolutionState with
                {
                    IsExecutingAsyncTaskLinks = inDotNetSolutionState.IsExecutingAsyncTaskLinks - 1
                }));
        }

        [EffectMethod(typeof(ParseDotNetSolutionAction))]
        public Task HandleParseDotNetSolutionAction(
            IDispatcher dispatcher)
        {
            var dotNetSolutionState = _dotNetSolutionStateWrap.Value;

            var dotNetSolutionModel = dotNetSolutionState.DotNetSolutions.FirstOrDefault(x =>
                x.DotNetSolutionModelKey == dotNetSolutionState.DotNetSolutionModelKey);

            if (dotNetSolutionModel is null)
                return Task.CompletedTask;

            //var parserTask = new ParserTask(
            //    async cancellationToken =>
            //    {
            //        dispatcher.Dispatch(new WithAction(inDotNetSolutionState =>
            //            inDotNetSolutionState with
            //            {
            //                IsExecutingAsyncTaskLinks = inDotNetSolutionState.IsExecutingAsyncTaskLinks + 1
            //            }));

            //        await Task.Delay(5_000);

            //        dispatcher.Dispatch(new WithAction(inDotNetSolutionState =>
            //            inDotNetSolutionState with
            //            {
            //                IsExecutingAsyncTaskLinks = inDotNetSolutionState.IsExecutingAsyncTaskLinks - 1
            //            }));
            //    },
            //    "Parse Task Name",
            //    "Parse Task Description",
            //    false,
            //    _ => Task.CompletedTask,
            //    dispatcher,
            //    CancellationToken.None);

            //_parserTaskQueue.QueueParserWorkItem(parserTask);

            return Task.CompletedTask;
        }
    }
}