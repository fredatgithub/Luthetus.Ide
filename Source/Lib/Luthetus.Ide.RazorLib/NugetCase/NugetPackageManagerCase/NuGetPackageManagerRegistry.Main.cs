﻿using Fluxor;
using Luthetus.CompilerServices.Lang.DotNetSolution;
using System.Collections.Immutable;

namespace Luthetus.Ide.RazorLib.NugetCase.NugetPackageManagerCase;

[FeatureState]
public partial record NuGetPackageManagerRegistry(
    IDotNetProject? SelectedProjectToModify,
    string NugetQuery,
    bool IncludePrerelease,
    ImmutableArray<NugetPackageRecord> MostRecentQueryResult)
{
    public NuGetPackageManagerRegistry()
        : this(
            default(IDotNetProject?),
            string.Empty,
            false,
            ImmutableArray<NugetPackageRecord>.Empty)
    {

    }
}