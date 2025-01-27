﻿using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.TextEditor.RazorLib.Lexes.Models;
using Luthetus.TextEditor.RazorLib.TextEditors.Models;

namespace Luthetus.TextEditor.RazorLib.TextEditors.States;

public partial class TextEditorViewModelState
{
    public record DisposeAction(Key<TextEditorViewModel> ViewModelKey);

    public record RegisterAction(
        Key<TextEditorViewModel> ViewModelKey,
        ResourceUri ResourceUri,
        ITextEditorService TextEditorService);

    public record SetViewModelWithAction(
        Key<TextEditorViewModel> ViewModelKey,
        Func<TextEditorViewModel, TextEditorViewModel> WithFunc);
}