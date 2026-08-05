using Nuke.Common;
using Nuke.Common.IO;
using System.Collections.Generic;
using System.Linq;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

/// <summary>
/// Runs the whole suite exactly once per build, into one results directory.
/// </summary>
/// <remarks>
/// This is the only place the suite is executed. Reports that other targets need are asked for HERE, as
/// extra outputs of that run - see <see cref="CollectsCoverage" /> and <see cref="ICoverageTarget" /> -
/// because a second <c>dotnet test</c> costs the full suite again and, on a database-backed suite, two
/// runs back to back exhaust the server's connection slots.
/// </remarks>
public interface ITestTarget : ICompileTarget
{
    sealed AbsolutePath TestResultsDirectory => NukeBuild.RootDirectory / "artifacts" / "test-results";

    /// <summary>
    /// Whether the run is instrumented, so it emits cobertura reports alongside the trx one.
    /// </summary>
    /// <remarks>
    /// False here and true for <see cref="ICoverageTarget" />: a repository that gates on coverage gets the
    /// reports out of the run it already pays for, and one that does not gate never pays for instrumentation
    /// it would throw away.
    /// </remarks>
    bool CollectsCoverage => false;

    Target Test =>
        _ => _.Description("Runs tests via Microsoft.Testing.Platform (MTP)")
            .DependsOn(Compile)
            .Executes(() =>
            {
                TestResultsDirectory.CreateOrCleanDirectory();

                DotNet(TestArguments(Solution.Path, Configuration.ToString(), TestResultsDirectory, CollectsCoverage));
            });

    /// <summary>
    /// The single <c>dotnet test</c> command line: one execution of the suite carrying every report that
    /// was asked of it.
    /// </summary>
    /// <remarks>
    /// MTP needs <c>--solution</c> for a .slnx and <c>--report-xunit-trx</c>; it rejects the VSTest
    /// <c>--logger trx</c>. Test projects build as Exe with UseMicrosoftTestingPlatformRunner. Coverage is
    /// directed by <c>--results-directory</c> alone - a solution-wide run is one process per test assembly,
    /// and <c>--coverage-output</c> would hand all of them the same file to write.
    /// </remarks>
    static string TestArguments(string solution, string configuration, string resultsDirectory, bool withCoverage)
    {
        List<string> arguments =
        [
            "test", "--solution", solution, "--configuration", configuration, "--no-build",
            "--results-directory", resultsDirectory, "--report-xunit-trx",
        ];

        if (withCoverage)
            arguments.AddRange(["--coverage", "--coverage-output-format", "cobertura"]);

        return string.Join(" ", arguments.Select(Quote));
    }

    /// <summary>Quotes what a shell would otherwise split - a checkout under a path with a space in it.</summary>
    private static string Quote(string argument) => argument.Contains(' ') ? $"\"{argument}\"" : argument;
}
