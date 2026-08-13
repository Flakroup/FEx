using Nuke.Common.IO;
using Shouldly;
using System.Linq;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The RID-specific publish pass must ADD to the solution build, never replace it.
/// <para>
/// Measured on the replacing version: <c>Test PublishApp --publish-runtime linux-x64</c> reported
/// "Build succeeded" over a deliberately failing test. Restore and Compile covered only the publish
/// projects, nothing references the test projects, and <c>dotnet test --no-build</c> does not fail on
/// missing input - it runs whatever assemblies are already on disk. A stale green is worse than a red.
/// </para>
/// </summary>
public sealed class CompileScopeTests
{
    private static readonly AbsolutePath Solution = (AbsolutePath)"/repo/Sample.slnx";
    private static readonly AbsolutePath App = (AbsolutePath)"/repo/src/Sample.App/Sample.App.csproj";
    private static readonly AbsolutePath Worker = (AbsolutePath)"/repo/src/Sample.Worker/Sample.Worker.csproj";

    private static readonly AppPublishEntry[] Entries =
    [
        new(App, (AbsolutePath)"/repo/artifacts/publish/Sample.App"),
        new(Worker, (AbsolutePath)"/repo/artifacts/publish/Sample.Worker")
    ];

    [Fact]
    public void AnOrdinaryRun_CoversTheSolutionAndNothingElse()
    {
        ICompileTarget.Scope(Solution, runtimeSpecific: false, Entries)
            .ShouldBe([(Solution, false)]);
    }

    [Fact]
    public void ARuntimeRun_CoversTheSolutionFirst_ThenEachPublishProjectWithTheRid()
    {
        // The solution pass is what Test and Pack later consume with --no-build; the RID passes are what
        // `dotnet publish --no-build -r` needs under obj/{Configuration}/{TFM}/{RID}/.
        ICompileTarget.Scope(Solution, runtimeSpecific: true, Entries)
            .ShouldBe([(Solution, false), (App, true), (Worker, true)]);
    }

    [Fact]
    public void TheSolutionIsCoveredWhateverElseTheRunDoes()
    {
        // The regression, stated directly: no combination of inputs may drop the solution from the scope.
        foreach (var runtimeSpecific in new[] { true, false })
            ICompileTarget.Scope(Solution, runtimeSpecific, Entries)
                .ShouldContain((Solution, false), $"runtimeSpecific: {runtimeSpecific}");
    }

    [Fact]
    public void TheSolutionPassNeverCarriesTheRuntime()
    {
        // A RID on the solution pass would build every project in the solution runtime-specific, which is
        // both slower and not what the non-published projects want.
        ICompileTarget.Scope(Solution, runtimeSpecific: true, Entries)
            .Single(step => step.Project == Solution)
            .WithRuntime.ShouldBeFalse();
    }

    [Fact]
    public void NoPublishEntries_StillCoversTheSolution()
    {
        // FEx's own build: PublishProjects is empty. The narrowing version issued no dotnet command at all
        // here and reported success over a solution it never built.
        ICompileTarget.Scope(Solution, runtimeSpecific: true, [])
            .ShouldBe([(Solution, false)]);
    }
}
