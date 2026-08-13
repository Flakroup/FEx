using Nuke.Common;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// Resolving the version runs GitVersion - an external process, and the one the release gates hang off.
/// Two things follow, and both were wrong: nothing may resolve it merely to REPORT it, and resolving it
/// repeatedly must not re-launch the tool. Measured on CI run 30262574981: six launches in one publish.
/// </summary>
public sealed class VersionResolutionCostTests
{
    [Fact]
    public void TheParameterListingDoesNotResolveTheVersion()
    {
        // The listing reads every [Parameter] by reflection, and VersionInfo is one of them - so the
        // banner printed at start-up was launching GitVersion before any target ran. Here the tool is not
        // even installed, so resolving would throw rather than merely cost a second.
        var build = new ListingBuild();

        var entries = build.Entries();

        entries.ShouldContain(e => e.Name == "GitVersionInfo" && Equals(e.Value, FExBuild.UnresolvedVersion));
    }

    [Fact]
    public void TheListingSurvivesAnEnvironmentWithoutGitVersion()
    {
        // The whole listing used to die on the one parameter it could not read.
        Should.NotThrow(() => new ListingBuild().Entries().ToList());
    }

    [Fact]
    public void TheVersionParameterIsStillReportedOnceSomethingHasResolvedIt()
    {
        // Skipping it is about not TRIGGERING the resolve - the value is still worth printing when a
        // target that genuinely needed the version has already paid for it.
        FExBuild.UnresolvedVersion.ShouldNotBeNullOrWhiteSpace();

        typeof(IGitVersionComponent)
            .GetProperty(nameof(IGitVersionComponent.IsVersionResolved),
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .ShouldNotBeNull("the listing needs a way to ask without resolving");
    }

    /// <summary>
    /// The memoisation that turns repeated reads into one launch. Asserted on the source shape rather than
    /// by counting processes: the component resolves through a static field, and a getter that dropped it
    /// would go back to launching the tool per read - which no assertion on a single value would notice.
    /// </summary>
    [Fact]
    public void TheResolvedVersionIsHeldSoRepeatedReadsDoNotRelaunchTheTool()
    {
        typeof(IGitVersionComponent)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .ShouldContain(f => f.FieldType == typeof(GitVersionInfo));
    }

    private sealed class ListingBuild : FExBuild, IGitVersionComponent
    {
        public override IEnumerable<string> PublishProjects { get; } = [];

        public IEnumerable<(string Name, object? Value)> Entries() =>
            GetParameterEntries(new(StringComparer.OrdinalIgnoreCase)).ToList();
    }
}
