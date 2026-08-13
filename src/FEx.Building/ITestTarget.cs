using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
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
    /// What MTP returns when an assembly matched no test at all - a narrowed run, not a failure.
    /// </summary>
    const int NoTestsRanExitCode = 8;

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
    /// both kinds - measured, it answers "expects at most 1 argument" and runs nothing - and
    /// <see cref="AdditionalTestArguments" /> is where a repository contributes its own simple filters.
    /// </para>
    /// <para>
    /// NUKE resolves this from the environment and from <c>.nuke/parameters.json</c> as well as from the
    /// command line, matching a variable name after stripping every non-alphanumeric character and ignoring
    /// case - <c>TEST_FILTER</c> and <c>NUKE_TEST_FILTER</c> set it as surely as the switch does. The floor
    /// in <see cref="Test" /> catches a filter that matched NOTHING; it does not catch one that matched
    /// something smaller than the suite, so a build that must run everything says so itself rather than
    /// relying on this.
    /// </para>
    /// </remarks>
    string? TestFilter { get; }

    /// <summary>
    /// Whether this repository deliberately leaves some of its test assemblies with nothing to run, so an
    /// assembly reporting that it matched no test is an intention rather than a failure.
    /// </summary>
    /// <remarks>
    /// A switch rather than a list of exit codes to forgive, and the narrowness is the point: the only code
    /// worth forgiving is the one that says a run was narrowed. An open set invites forgiving
    /// <c>AtLeastOneTestFailed</c> to get past a flaky suite, and from then on every red run reports green -
    /// the one outcome a build gate exists to prevent.
    /// <para>
    /// It forgives an assembly, never the run: <see cref="Test" /> still fails when NOTHING executed
    /// anywhere.
    /// </para>
    /// </remarks>
    bool ForgivesEmptyAssemblies => false;

    Target Test =>
        _ => _.Description("Runs tests via Microsoft.Testing.Platform (MTP)").DependsOn(Compile).Executes(OnTest);

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
    static string TestArguments(string solution,
                                string configuration,
                                string resultsDirectory,
                                bool withCoverage,
                                string? testFilter,
                                IEnumerable<string>? additionalArguments,
                                bool forgivesEmptyAssemblies)
    {
        List<string> arguments =
        [
            "test", "--solution", solution, "--configuration", configuration, "--no-build",
            "--results-directory", resultsDirectory, "--report-xunit-trx"
        ];

        if (withCoverage)
            arguments.AddRange(["--coverage", "--coverage-output-format", "cobertura"]);

        var filtered = !string.IsNullOrWhiteSpace(testFilter);

        if (filtered)
            arguments.AddRange(["--filter-class", testFilter!]);

        if (additionalArguments is not null)
            arguments.AddRange(additionalArguments);

        // One process per assembly, so narrowing the run leaves every assembly the narrowing missed with
        // nothing to do, and each reports it. Emitted once, because MTP takes the option exactly once.
        if (filtered || forgivesEmptyAssemblies)
            arguments.AddRange(["--ignore-exit-code", NoTestsRanExitCode.ToString(CultureInfo.InvariantCulture)]);

        return string.Join(" ", arguments.Select(Quote));
    }

    /// <summary>
    /// Whether the run knew of no test at all, across every report it wrote.
    /// </summary>
    /// <remarks>
    /// Reads <c>total</c> rather than <c>executed</c>: a run whose tests were all skipped still MATCHED
    /// them, and the question here is whether the narrowing found anything, not whether it ran. No reports
    /// at all counts as nothing - a run that wrote none did not get far enough to have found tests.
    /// </remarks>
    static bool MatchedNothing(IEnumerable<XDocument> reports) =>
        reports.Sum(static report => report.Descendants()
            .Where(static element => element.Name.LocalName == "Counters")
            .Sum(static counters => (int?)counters.Attribute("total") ?? 0))
        == 0;

    /// <summary>
    /// MTP arguments this repository's suite needs on top of the standard command line, as individual
    /// tokens - a flag and its value are two entries, so quoting stays this method's business and not the
    /// contributor's.
    /// </summary>
    /// <remarks>
    /// This is the seam that keeps the run defined ONCE. A repository with a suite to trim overrides this
    /// and the target body stays here; replacing <see cref="Test" /> instead forks it, and the fork then
    /// silently misses every report, environment variable or retry added here afterward. A method rather
    /// than a property because an override may legitimately announce what it is leaving out, and a property
    /// getter that logs surprises its reader.
    /// <para>
    /// Do NOT contribute <c>--ignore-exit-code</c> here - see <see cref="ForgivesEmptyAssemblies" />. MTP
    /// declares that option with an arity of exactly one and sums arity across repeated occurrences, so a
    /// second copy aborts the run before a test executes.
    /// </para>
    /// </remarks>
    IEnumerable<string> AdditionalTestArguments() => [];

    IReadOnlyCollection<Output> OnTest()
    {
        TestResultsDirectory.CreateOrCleanDirectory();

        var result = DotNet(TestCommandLine(Solution.Path, Configuration.ToString(), TestResultsDirectory));

        // The floor under every narrowing this target allows. Forgiving NoTestsRan is what makes a
        // filter usable at all, and it is also what makes "matched one class" and "matched nothing"
        // the same green outcome - measured: a mistyped filter reported a successful build having
        // executed zero tests, with nothing above debug level to say so. A run that executed
        // nothing ANYWHERE is never what the caller meant, whether the filter came from the command
        // line, the environment or a parameters file.
        if (MatchedNothing(TestResultsDirectory.GlobFiles("*.trx").Select(static trx => XDocument.Load(trx))))
            throw new InvalidOperationException(
                $"The test run matched no test at all. Filter: '{TestFilter ?? "(none)"}'; extra "
                + $"arguments: '{string.Join(' ', AdditionalTestArguments())}'. Whichever of those "
                + "narrowed the run, it narrowed it to nothing. A filter names test CLASSES and "
                + "takes wildcards - '*OrderTests', not 'OrderTests', and one pattern rather than "
                + "several separated by spaces.");

        return result;
    }

    /// <summary>The command line this build's seams compose, given the run's ambient values.</summary>
    /// <remarks>
    /// Extracted from the target body so the composition is reachable from a test - it was not, and every
    /// seam could be deleted from the call with the suite staying green. What a test can now reach is this
    /// method; the target's single call to it is still beyond reach, because exercising a NUKE target needs
    /// NUKE. The parameters are required rather than optional so that dropping one is a compile error
    /// instead of a silent narrowing, which is the nearest thing to a test that this boundary can have.
    /// The ambient values stay parameters because a build constructed in a test has no solution.
    /// </remarks>
    sealed string TestCommandLine(string solution, string configuration, string resultsDirectory) =>
        TestArguments(solution,
            configuration,
            resultsDirectory,
            CollectsCoverage,
            TestFilter,
            AdditionalTestArguments(),
            ForgivesEmptyAssemblies);

    /// <summary>Quotes what a shell would otherwise split - a checkout under a path with a space in it.</summary>
    private static string Quote(string argument) =>
        argument.Contains(' ')
            ? $"\"{argument}\""
            : argument;
}