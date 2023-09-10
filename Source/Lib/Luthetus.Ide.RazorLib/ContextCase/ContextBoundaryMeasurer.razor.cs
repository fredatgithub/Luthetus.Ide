using Microsoft.AspNetCore.Components;
using Fluxor.Blazor.Web.Components;
using Luthetus.Ide.ClassLib.Store.ContextCase;
using Fluxor;
using Luthetus.Ide.ClassLib.Context;
using Microsoft.JSInterop;
using Luthetus.Ide.ClassLib.JavaScriptObjects;
using System.Collections.Immutable;

namespace Luthetus.Ide.RazorLib.ContextCase;

public partial class ContextBoundaryMeasurer : FluxorComponent
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject]
    private IState<ContextStates> ContextStatesWrap { get; set; } = null!;
    [Inject]
    private IDispatcher Dispatcher { get; set; } = null!;

    [Parameter, EditorRequired]
    public ContextRecord ContextRecord { get; set; } = null!;
    [Parameter, EditorRequired]
    public Func<ImmutableArray<ContextRecord>> GetContextBoundaryHeirarchy { get; set; } = null!;

    private bool _previousIsSelectingInspectionTarget;

    protected override bool ShouldRender()
    {
        var contextStates = ContextStatesWrap.Value;

        if (_previousIsSelectingInspectionTarget != contextStates.IsSelectingInspectionTarget)
            return true;

        return false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var contextStates = ContextStatesWrap.Value;

        if (_previousIsSelectingInspectionTarget != contextStates.IsSelectingInspectionTarget)
        {
            _previousIsSelectingInspectionTarget = contextStates.IsSelectingInspectionTarget;

            if (contextStates.IsSelectingInspectionTarget)
            {
                var measuredHtmlElementDimensions = await JsRuntime.InvokeAsync<MeasuredHtmlElementDimensions>("luthetusIde.measureElementById",
                    ContextRecord.ContextElementId);

                var contextBoundaryHeirarchy = GetContextBoundaryHeirarchy.Invoke();

                measuredHtmlElementDimensions = measuredHtmlElementDimensions with
                {
                    ZIndex = contextBoundaryHeirarchy.Length
                };

                Dispatcher.Dispatch(new ContextStates.AddMeasuredHtmlElementDimensionsAction(
                    ContextRecord, contextBoundaryHeirarchy, measuredHtmlElementDimensions));
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}