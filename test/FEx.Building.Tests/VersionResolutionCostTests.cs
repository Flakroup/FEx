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
    /// <summary>
    /// The version is NOT a NUKE parameter, and must not become one again. While it was, the start-up
    /// listing read it by reflection and so launching GitVersion became a side effect of printing the
    /// banner - `Clean` paid for a version it never used, and on CI the release gates fired during
    /// initialisation, before any target ran. The attribute bought nothing in return: NUKE's argument
    /// parser is string-to-scalar and cannot build this record from a command line or an environment
    /// variable, so the only route it ever opened was a .nuke parameters file nobody writes.
    /// </summary>
    [Fact]
    public void TheVersionIsNotAParameter_SoPrintingCannotLaunchGitVersion()
    {
        typeof(IGitVersionComponent)
            .GetProperty(nameof(IGitVersionComponent.VersionInfo),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ShouldNotBeNull()
            .GetCustomAttribute<ParameterAttribute>()
            .ShouldBeNull("a [Parameter] here makes the banner resolve the version");
    }

    [Fact]
    public void TheParameterListingNeverMentionsTheVersion()
    {
        // The end the attribute test protects: whatever the listing enumerates, the version is not in it,
        // so no reflective read of it can happen while printing.
        var build = new ListingBuild();

        build.Entries().ShouldNotContain(static e => e.Name == "GitVersionInfo");
    }

    [Fact]
    public void TheListingSurvivesAnEnvironmentWithoutGitVersion()
    {
        // The whole listing used to die on the one parameter it could not read.
        Should.NotThrow(() => new ListingBuild().Entries().ToList());
    }

    /// <summary>
    /// The memoisation that turns repeated reads into one launch. Driven through the property rather than
    /// asserted on the shape of the field: a getter that keeps the field but drops the <c>??=</c> still
    /// re-launches the tool on every read - the six-launch defect - and a shape assertion stays green.
    /// </summary>
    [Fact]
    public void ASecondReadReusesTheResolvedVersionInsteadOfRelaunchingTheTool()
    {
        var field = ResolvedVersionField();
        var previous = field.GetValue(null);
        var seeded = new GitVersionInfo { SemVer = "9.9.9-probe" };

        try
        {
            field.SetValue(null, seeded);

            // With the memoisation in place this returns the seeded value without entering the resolver.
            // Without it, the resolver runs and throws here (GitVersion.Tool is not on the test host).
            ((IGitVersionComponent)new ListingBuild()).VersionInfo.ShouldBeSameAs(seeded);
        }
        finally
        {
            field.SetValue(null, previous);
        }
    }

    // Looked up by field TYPE: ReflectionAnalyzers rejects a name lookup for a private member (REFL003).
    private static FieldInfo ResolvedVersionField() =>
        typeof(IGitVersionComponent)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Single(static f => f.FieldType == typeof(GitVersionInfo));

    private sealed class ListingBuild : FExBuild, IGitVersionComponent
    {
        public override IEnumerable<string> PublishProjects { get; } = [];

        public IEnumerable<(string Name, object? Value)> Entries() =>
            GetParameterEntries(new(StringComparer.OrdinalIgnoreCase)).ToList();
    }
}
