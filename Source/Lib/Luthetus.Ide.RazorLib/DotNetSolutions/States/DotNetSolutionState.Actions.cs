﻿using Luthetus.CompilerServices.Lang.DotNetSolution.RewriteForImmutability;
using Luthetus.Common.RazorLib.Keys.Models;

namespace Luthetus.Ide.RazorLib.DotNetSolutions.States;

public partial record DotNetSolutionState
{
    public record RegisterAction(DotNetSolutionModel DotNetSolutionModel, DotNetSolutionSync Sync);
    public record DisposeAction(Key<DotNetSolutionModel> DotNetSolutionModelKey, DotNetSolutionSync Sync);
}