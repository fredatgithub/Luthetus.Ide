using Fluxor;

namespace Luthetus.Ide.RazorLib.TerminalCase.States;

public partial record WellKnownTerminalSessionsState
{
    private class Reducer
    {
        [ReducerMethod]
        public static WellKnownTerminalSessionsState ReduceSetActiveTerminalCommandKeyAction(
            WellKnownTerminalSessionsState inWellKnownTerminalSessionsState,
            WellKnownTerminalSessionsState.SetActiveWellKnownTerminalSessionKey setActiveWellKnownTerminalSessionKeyAction)
        {
            return inWellKnownTerminalSessionsState with
            {
                ActiveTerminalSessionKey = setActiveWellKnownTerminalSessionKeyAction.TerminalSessionKey
            };
        }
    }
}