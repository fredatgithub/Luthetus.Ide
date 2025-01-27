using Fluxor;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.Ide.RazorLib.Terminals.Models;

namespace Luthetus.Ide.RazorLib.Terminals.States;

[FeatureState]
public partial record WellKnownTerminalSessionsState(Key<TerminalSession> ActiveTerminalSessionKey)
{
    public WellKnownTerminalSessionsState() : this(TerminalSessionFacts.GENERAL_TERMINAL_SESSION_KEY)
    {
    }
}
