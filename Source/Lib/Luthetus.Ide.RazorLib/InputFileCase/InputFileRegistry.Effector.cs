﻿using Fluxor;
using Luthetus.Common.RazorLib.Dialog;
using Luthetus.Common.RazorLib.Store.DialogCase;
using Luthetus.Ide.RazorLib.HtmlCase;
using Luthetus.Ide.RazorLib.ComponentRenderersCase;

namespace Luthetus.Ide.RazorLib.InputFileCase;

public partial record InputFileRegistry
{
    private class Effector
    {
        private readonly ILuthetusIdeComponentRenderers _luthetusIdeComponentRenderers;

        public Effector(
            ILuthetusIdeComponentRenderers luthetusIdeComponentRenderers)
        {
            _luthetusIdeComponentRenderers = luthetusIdeComponentRenderers;
        }

        [EffectMethod]
        public Task HandleRequestInputFileStateFormAction(
            RequestInputFileStateFormAction requestInputFileStateFormAction,
            IDispatcher dispatcher)
        {
            if (_luthetusIdeComponentRenderers.InputFileRendererType is not null)
            {
                dispatcher.Dispatch(new StartInputFileStateFormAction(
                    requestInputFileStateFormAction));

                var inputFileDialog = new DialogRecord(
                    DialogFacts.InputFileDialogKey,
                    "Input File",
                    _luthetusIdeComponentRenderers.InputFileRendererType,
                    null,
                    HtmlFacts.Classes.DIALOG_PADDING_0)
                {
                    IsResizable = true
                };

                dispatcher.Dispatch(new DialogRegistry.RegisterAction(
                    inputFileDialog));
            }

            return Task.CompletedTask;
        }
    }
}