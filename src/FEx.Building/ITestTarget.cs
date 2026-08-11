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
    /// <summary>
    /// What MTP returns when an assembly matched no test at all - a filtered or trimmed run, not a failure.
    /// </summary>
    public const int NoTestsRanExitCode = 8;

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

    /// <summary>
    /// Narrows the run to the test classes whose fully qualified name matches, for the edit-run loop.
    /// </summary>
    /// <remarks>
    /// MTP has no single <c>--filter</c>; it has a family, and this maps to <c>--filter-class</c> because
    /// running one class is what the loop asks for. The value reaches the runner verbatim, so the caller
    /// owns the wildcards - <c>*SomeTests</c> rather than a bare name, which would have to be fully
    /// qualified to match anything.
    /// <para>
    /// Deliberately a SIMPLE filter rather than <c>--filter-query</c>: MTP refuses a command line carrying
    /// both kinds, and <see cref="AdditionalTestArguments" /> is where a repository contributes its own
    /// simple filters. A query here would break every build that trims its suite.
    /// </para>
    /// </remarks>
    [Parameter("Run only test classes matching this name - wildcards with '*' (e.g. '*OrderTests')")]
    string? TestFilter => TryGetValue(() => TestFilter);

    /// <summary>
    /// MTP arguments this repository's suite needs on top of the standard command line, as individual
    /// tokens - a flag and its value are two entries, so quoting stays the caller's business and not
    /// the contributor's.
    /// </summary>
    /// <remarks>
    /// This is the seam that keeps the run defined ONCE. A repository with a suite to trim or an exit code
    /// to forgive overrides this and the target body stays here; replacing <see cref="Test" /> instead
    /// forks it, and the fork then silently misses every report, environment variable or retry added here
    /// afterwards. A method rather than a property because an override may legitimately announce what it
    /// is doing, and a property getter that logs surprises its reader.
    /// <para>
    /// Exit codes are NOT contributed here - see <see cref="IgnoredTestExitCodes" />. MTP takes them as one
    /// semicolon-separated list on a single flag, so two contributors emitting the flag would produce a
    /// command line MTP rejects.
    /// </para>
    /// </remarks>
    IEnumerable<string> AdditionalTestArguments() => [];

    /// <summary>
    /// Exit codes from the runner that this repository's run is expected to produce, and which therefore
    /// do not fail the build.
    /// </summary>
    /// <remarks>
    /// Merged with whatever the run itself makes unavoidable and emitted once, because
    /// <c>--ignore-exit-code</c> takes a semicolon-separated list rather than repeating.
    /// <para>
    /// This forgives an EXPECTED SHAPE of run, never a failing test: MTP reports a failed assertion with a
    /// different code, and listing 8 here does not make a red suite green.
    /// </para>
    /// </remarks>
    IEnumerable<int> IgnoredTestExitCodes => [];

    Target Test =>
        _ => _.Description("Runs tests via Microsoft.Testing.Platform (MTP)")
            .DependsOn(Compile)
            .Executes(() =>
            {
                TestResultsDirectory.CreateOrCleanDirectory();

                DotNet(TestArguments(
                    Solution.Path,
                    Configuration.ToString(),
                    TestResultsDirectory,
                    CollectsCoverage,
                    TestFilter,
                    AdditionalTestArguments(),
                    IgnoredTestExitCodes));
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
    static string TestArguments(
        string solution,
        string configuration,
        string resultsDirectory,
        bool withCoverage,
        string? testFilter = null,
        IEnumerable<string>? additionalArguments = null,
        IEnumerable<int>? ignoredExitCodes = null)
    {
        List<string> arguments =
        [
            "test", "--solution", solution, "--configuration", configuration, "--no-build",
            "--results-directory", resultsDirectory, "--report-xunit-trx",
        ];

        if (withCoverage)
            arguments.AddRange(["--coverage", "--coverage-output-format", "cobertura"]);

        SortedSet<int> ignored = new(ignoredExitCodes ?? []);

        if (!string.IsNullOrWhiteSpace(testFilter))
        {
            arguments.AddRange(["--filter-class", testFilter]);

            // A solution-wide run is one process per test assembly, and a filter naming one class leaves
            // every OTHER assembly matching nothing. Each of those exits NoTestsRan and the run is reported
            // failed - measured, and it makes an unforgiven filter useless rather than merely noisy. This
            // is part of filtering, not a caller's problem, so it is not left to IgnoredTestExitCodes.
            ignored.Add(NoTestsRanExitCode);
        }

        if (additionalArguments is not null)
            arguments.AddRange(additionalArguments);

        if (ignored.Count > 0)
            arguments.AddRange(["--ignore-exit-code", string.Join(";", ignored)]);

        return string.Join(" ", arguments.Select(Quote));
    }

    /// <summary>Quotes what a shell would otherwise split - a checkout under a path with a space in it.</summary>
    private static string Quote(string argument) => argument.Contains(' ') ? $"\"{argument}\"" : argument;
}
