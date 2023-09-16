using Luthetus.Common.RazorLib.KeyCase;
using Luthetus.Ide.RazorLib.TerminalCase.Models;

namespace Luthetus.Ide.RazorLib.TerminalCase.States;

public partial record WellKnownTerminalSessionsState
{
    public record SetActiveWellKnownTerminalSessionKey(Key<TerminalSession> TerminalSessionKey);
}