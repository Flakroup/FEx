using Nuke.Common;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The suite is executed once per build and every report is an output of that one run. These pin the
/// command line that makes it so - the defect they guard against is the coverage gate starting a second
/// <c>dotnet test</c>, which doubled the wall clock and, on a database-backed suite, ran the server out
/// of connection slots.
/// </summary>
public sealed class TestTargetTests
{
    [Fact]
    public void InstrumentedRun_AsksForTheTrxAndTheCoberturaReportsAtOnce()
    {
        var arguments = Arguments(withCoverage: true);

        arguments.ShouldContain("--report-xunit-trx");
        arguments.ShouldContain("--coverage");
        arguments.ShouldContain("--coverage-output-format cobertura");
    }

    [Fact]
    public void InstrumentedRun_IsStillOneRun()
    {
        // One `dotnet test`, one suite: a single verb and a single results directory for both reports.
        var arguments = Arguments(withCoverage: true);

        Occurrences(arguments, "--solution").ShouldBe(1);
        Occurrences(arguments, "--results-directory").ShouldBe(1);
        arguments.ShouldStartWith("test ");
    }

    [Fact]
    public void PlainRun_IsNotInstrumented()
    {
        // A repository without a coverage gate pays for no instrumentation and gets no reports to ignore.
        var arguments = Arguments(withCoverage: false);

        arguments.ShouldNotContain("--coverage");
        arguments.ShouldContain("--report-xunit-trx");
    }

    [Fact]
    public void EveryRun_BuildsNothing_BecauseCompileAlreadyDid()
    {
        Arguments(withCoverage: true).ShouldContain("--no-build");
        Arguments(withCoverage: false).ShouldContain("--no-build");
    }

    [Fact]
    public void SolutionAndResultsDirectory_ReachTheRunner()
    {
        var arguments = Arguments(withCoverage: true);

        arguments.ShouldContain("--solution C:/repo/Some.slnx");
        arguments.ShouldContain("--results-directory C:/repo/artifacts/test-results");
        arguments.ShouldContain("--configuration Release");
    }

    [Fact]
    public void PathWithASpace_IsQuoted_SoItStaysOneArgument()
    {
        var arguments = ITestTarget.TestArguments(
            @"C:\src\my repo\Some.slnx",
            "Debug",
            @"C:\src\my repo\artifacts\test-results",
            withCoverage: true,
            testFilter: null,
            additionalArguments: null,
            forgivesEmptyAssemblies: false);

        arguments.ShouldContain(@"--solution ""C:\src\my repo\Some.slnx""");
        arguments.ShouldContain(@"--results-directory ""C:\src\my repo\artifacts\test-results""");
    }

    [Fact]
    public void TheCoverageGate_CannotBeDeclaredWithoutTheTestRun()
    {
        // The gate reads reports it does not produce, so the type system says where they come from.
        typeof(ITestTarget).IsAssignableFrom(typeof(ICoverageTarget)).ShouldBeTrue();
    }

    [Fact]
    public void ABuildWithTheGate_InstrumentsTheRunItAlreadyPaysFor()
    {
        ((ITestTarget)new GatedBuild()).CollectsCoverage.ShouldBeTrue();
    }

    [Fact]
    public void ABuildWithoutTheGate_RunsUninstrumented()
    {
        ((ITestTarget)new UngatedBuild()).CollectsCoverage.ShouldBeFalse();
    }

    [Fact]
    public void TestFilter_NarrowsTheRunToTheClassesThatMatch()
    {
        var arguments = Arguments(withCoverage: false, testFilter: "*OrderTests");

        arguments.ShouldContain("--filter-class *OrderTests");
    }

    [Fact]
    public void NoTestFilter_LeavesTheWholeSuiteRunning()
    {
        Arguments(withCoverage: false).ShouldNotContain("--filter-class");
    }

    [Fact]
    public void BlankTestFilter_IsNotAFilter()
    {
        // NUKE hands back an empty string for a parameter named without a value, and passing that on would
        // narrow the run to the classes called "" - a green build that tested nothing.
        Arguments(withCoverage: false, testFilter: "   ").ShouldNotContain("--filter-class");
    }

    [Fact]
    public void AdditionalArguments_ReachTheRunner()
    {
        // Deliberately NOT --ignore-exit-code: that option is the one thing the seam's own documentation
        // rules out, and a test presenting it as the example teaches the pattern that breaks the run.
        var arguments = Arguments(withCoverage: false, additional: ["--timeout", "5m"]);

        arguments.ShouldContain("--timeout 5m");
    }

    [Fact]
    public void ContributingExtraArguments_DoesNotCostTheStandardOnes()
    {
        // This is the property that makes the seam worth having. A repository trims its suite by adding
        // arguments and keeps every report, the results directory and --no-build; if it did not, forking
        // the whole target would be the only way to trim, and the fork would then miss whatever is added
        // here next.
        var arguments = Arguments(
            withCoverage: true, testFilter: "*OrderTests", additional: ["--filter-not-namespace", "Slow.Tests*"]);

        arguments.ShouldStartWith("test ");
        arguments.ShouldContain("--report-xunit-trx");
        arguments.ShouldContain("--coverage");
        arguments.ShouldContain("--no-build");
        arguments.ShouldContain("--results-directory C:/repo/artifacts/test-results");
        arguments.ShouldContain("--filter-class *OrderTests");
        arguments.ShouldContain("--filter-not-namespace Slow.Tests*");
    }

    [Fact]
    public void ABuildThatContributesNothing_ContributesNothing()
    {
        ((ITestTarget)new UngatedBuild()).AdditionalTestArguments().ShouldBeEmpty();
        ((ITestTarget)new UngatedBuild()).ForgivesEmptyAssemblies.ShouldBeFalse();
    }

    /// <summary>
    /// Every seam a build declares reaches the runner. Nothing pinned this before: deleting all three from
    /// the target's own call to the assembler left the whole suite green - measured - so the composition
    /// this PR exists to provide could be removed without a single red test.
    /// </summary>
    [Fact]
    public void EverySeamAContributingBuildDeclares_ReachesTheCommandLine()
    {
        var arguments = ((ITestTarget)new ContributingBuild())
            .TestCommandLine("C:/repo/Some.slnx", "Release", "C:/repo/artifacts/test-results");

        arguments.ShouldContain("--filter-class *OrderTests");
        arguments.ShouldContain("--filter-not-namespace Slow.Tests*");
        arguments.ShouldContain($"--ignore-exit-code {ITestTarget.NoTestsRanExitCode}");
    }

    /// <summary>
    /// The filter is a NUKE PARAMETER, not merely a property. Dropping the attribute leaves a build that
    /// silently ignores <c>--test-filter</c> and runs the whole suite - measured green across the suite,
    /// because nothing else reads the attribute.
    /// </summary>
    [Fact]
    public void TestFilter_IsBoundFromTheCommandLine_NotJustAProperty() =>
        typeof(ITestTarget).GetProperty(
                nameof(ITestTarget.TestFilter),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ShouldNotBeNull()
            .GetCustomAttribute<ParameterAttribute>()
            .ShouldNotBeNull();

    [Fact]
    public void AFilteredRun_ForgivesTheAssembliesItEmptied()
    {
        // Measured: a solution-wide run is one process per assembly, so a filter naming one class leaves
        // every other assembly matching nothing, each returning NoTestsRan. Unforgiven, the filter reports
        // a failed build however green the tests it actually ran - which is what it did before this line.
        Arguments(withCoverage: false, testFilter: "*OrderTests")
            .ShouldContain($"--ignore-exit-code {ITestTarget.NoTestsRanExitCode}");
    }

    [Fact]
    public void AWholeRunWithNothingContributed_ForgivesNothing()
    {
        // The whole suite running nothing is a broken build, not an intention - so the flag is absent
        // rather than present and empty, which would forgive that too.
        Arguments(withCoverage: false).ShouldNotContain("--ignore-exit-code");
    }

    [Fact]
    public void AnUnfilteredRun_StillForgivesTheAssembliesTheRepositoryEmptied()
    {
        // The consumer's own CI invocation: a trimmed suite with no filter. Nothing covered it - moving the
        // emit inside the filter branch dropped the option from every unfiltered run and the suite stayed
        // green, which is exactly how a trimmed CI would have gone red for no visible reason.
        Arguments(withCoverage: false, forgivesEmptyAssemblies: true)
            .ShouldContain($"--ignore-exit-code {ITestTarget.NoTestsRanExitCode}");
    }

    [Fact]
    public void ARunThatIsBothTrimmedAndFiltered_AsksOnce()
    {
        // MTP declares the option with an arity of exactly one and sums arity across occurrences, so a
        // second copy aborts the run before a test executes.
        var arguments = Arguments(
            withCoverage: false, testFilter: "*OrderTests", forgivesEmptyAssemblies: true);

        Occurrences(arguments, "--ignore-exit-code").ShouldBe(1);
    }

    /// <summary>
    /// The floor under every narrowing this target allows. Forgiving the emptied assemblies is what makes a
    /// filter usable and is also what makes "matched one class" and "matched nothing" the same green
    /// outcome - measured: a mistyped filter reported a successful build having executed zero tests.
    /// </summary>
    /// <summary>
    /// The one number here that is not ours to choose. Every other assertion in this file interpolates the
    /// constant, so they hold whatever it says; this one holds it to what the runner actually returns.
    /// Measured: a test assembly given a filter matching no class prints "Zero tests ran" and exits 8.
    /// </summary>
    [Fact]
    public void NoTestsRanExitCode_IsTheCodeTheRunnerActuallyReturns() =>
        ITestTarget.NoTestsRanExitCode.ShouldBe(8);

    [Fact]
    public void ARunThatMatchedNothingAnywhere_IsNotAPass() =>
        ITestTarget.MatchedNothing([Report(total: 0), Report(total: 0)]).ShouldBeTrue();

    [Fact]
    public void ARunWhereOneAssemblyMatched_IsAPass_HoweverManyOthersWereEmptied() =>
        ITestTarget.MatchedNothing([Report(total: 0), Report(total: 127), Report(total: 0)]).ShouldBeFalse();

    [Fact]
    public void ARunThatWroteNoReportAtAll_IsNotAPass() =>
        ITestTarget.MatchedNothing([]).ShouldBeTrue();

    /// <summary>A run whose tests were all skipped still MATCHED them - the floor asks whether the
    /// narrowing found anything, not whether it executed.</summary>
    [Fact]
    public void ARunWhoseTestsWereAllSkipped_StillMatchedThem() =>
        ITestTarget.MatchedNothing([Report(total: 3, executed: 0)]).ShouldBeFalse();

    /// <summary>The shape MTP actually writes, measured from a real run: one Counters element per report,
    /// carrying total on the attribute this reads.</summary>
    /// <summary>
    /// A report shaped like the ones MTP writes, NAMESPACE INCLUDED.
    /// </summary>
    /// <remarks>
    /// The namespace is the whole point of this fixture. Without it the obvious implementation -
    /// <c>Descendants("Counters")</c> - passes every test here and matches nothing at all against a real
    /// report, so every green suite would report that it had run no test. Verified against the reports of a
    /// real run: <c>&lt;TestRun … xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"&gt;</c>
    /// with one <c>Counters</c> element carrying <c>total</c>.
    /// </remarks>
    private static XDocument Report(int total, int? executed = null) =>
        XDocument.Parse(
            $"""<TestRun xmlns="{TrxNamespace}"><ResultSummary outcome="Completed"><Counters total="{total}" executed="{executed ?? total}" passed="{executed ?? total}" failed="0" /></ResultSummary></TestRun>""");

    private const string TrxNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    private static string Arguments(
        bool withCoverage,
        string? testFilter = null,
        IEnumerable<string>? additional = null,
        bool forgivesEmptyAssemblies = false) =>
        ITestTarget.TestArguments(
            "C:/repo/Some.slnx",
            "Release",
            "C:/repo/artifacts/test-results",
            withCoverage,
            testFilter,
            additional,
            forgivesEmptyAssemblies);

    private static int Occurrences(string arguments, string flag)
    {
        var count = 0;
        var at = arguments.IndexOf(flag, StringComparison.Ordinal);

        while (at >= 0)
        {
            count++;
            at = arguments.IndexOf(flag, at + flag.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private sealed class GatedBuild : FExBuild, ICoverageTarget;

    private sealed class UngatedBuild : FExBuild, ITestTarget;

    /// <summary>A repository that uses every seam at once - the shape the wiring test needs.</summary>
    private sealed class ContributingBuild : FExBuild, ITestTarget
    {
        public string? TestFilter => "*OrderTests";

        public bool ForgivesEmptyAssemblies => true;

        public IEnumerable<string> AdditionalTestArguments() => ["--filter-not-namespace", "Slow.Tests*"];
    }
}
