﻿using Fluxor;
using Luthetus.Common.RazorLib.Dialogs.States;
using Luthetus.Common.RazorLib.Dialogs.Models;
using Luthetus.Ide.RazorLib.InputFiles.States;
using Microsoft.AspNetCore.Components;
using Luthetus.Ide.RazorLib.InputFiles.Models;

namespace Luthetus.Ide.RazorLib.InputFiles.Displays;

public partial class InputFileBottomControls : ComponentBase
{
    [Inject]
    private IDispatcher Dispatcher { get; set; } = null!;

    [CascadingParameter]
    public DialogRecord? DialogRecord { get; set; }
    [CascadingParameter]
    public InputFileState InputFileState { get; set; } = null!;

    private string _searchQuery = string.Empty;

    private void SelectInputFilePatternOnChange(ChangeEventArgs changeEventArgs)
    {
        var patternName = (string)(changeEventArgs.Value ?? string.Empty);

        var pattern = InputFileState.InputFilePatternsBag
            .FirstOrDefault(x => x.PatternName == patternName);

        if (pattern is not null)
            Dispatcher.Dispatch(new InputFileState.SetSelectedInputFilePatternAction(pattern));
    }

    private string GetSelectedTreeViewModelAbsolutePathString(InputFileState inputFileState)
    {
        var selectedAbsolutePath = inputFileState.SelectedTreeViewModel?.Item;

        if (selectedAbsolutePath is null)
            return "Selection is null";

        return selectedAbsolutePath.Value;
    }

    private async Task FireOnAfterSubmit()
    {
        var valid = await InputFileState.SelectionIsValidFunc.Invoke(
            InputFileState.SelectedTreeViewModel?.Item);

        if (valid)
        {
            if (DialogRecord is not null)
                Dispatcher.Dispatch(new DialogState.DisposeAction(DialogRecord.Key));

            await InputFileState.OnAfterSubmitFunc.Invoke(InputFileState.SelectedTreeViewModel?.Item);
        }
    }

    private bool OnAfterSubmitIsDisabled()
    {
        return !InputFileState.SelectionIsValidFunc.Invoke(InputFileState.SelectedTreeViewModel?.Item)
            .Result;
    }

    private Task CancelOnClick()
    {
        if (DialogRecord is not null)
            Dispatcher.Dispatch(new DialogState.DisposeAction(DialogRecord.Key));

        return Task.CompletedTask;
    }
    
    private bool GetInputFilePatternIsSelected(InputFilePattern inputFilePattern, InputFileState localInputFileState)
    {
        return (localInputFileState.SelectedInputFilePattern?.PatternName ?? string.Empty) ==
            inputFilePattern.PatternName;
    }
}