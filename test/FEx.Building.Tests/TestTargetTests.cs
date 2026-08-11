using Shouldly;
using System;
using System.Collections.Generic;
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
            withCoverage: true);

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
        var arguments = Arguments(withCoverage: false, additional: ["--ignore-exit-code", "8"]);

        arguments.ShouldContain("--ignore-exit-code 8");
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
    public void ABuildThatContributesNothing_GetsTheStandardCommandLine()
    {
        ((ITestTarget)new UngatedBuild()).AdditionalTestArguments().ShouldBeEmpty();
        ((ITestTarget)new UngatedBuild()).IgnoredTestExitCodes.ShouldBeEmpty();
    }

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
    public void IgnoredExitCodes_AreOneFlag_BecauseMtpTakesAListAndNotARepeat()
    {
        var arguments = Arguments(withCoverage: false, testFilter: "*OrderTests", ignoredExitCodes: [8, 9]);

        Occurrences(arguments, "--ignore-exit-code").ShouldBe(1);
        arguments.ShouldContain("--ignore-exit-code 8;9");
    }

    [Fact]
    public void ACodeTheRunAlreadyForgives_IsNotListedTwice()
    {
        // A build that trims its suite AND filters asks for NoTestsRan from both sides; MTP reads the list
        // once, and "8;8" is a command line nobody wrote on purpose.
        var arguments = Arguments(
            withCoverage: false,
            testFilter: "*OrderTests",
            ignoredExitCodes: [ITestTarget.NoTestsRanExitCode]);

        arguments.ShouldContain($"--ignore-exit-code {ITestTarget.NoTestsRanExitCode}");
        arguments.ShouldNotContain($"{ITestTarget.NoTestsRanExitCode};{ITestTarget.NoTestsRanExitCode}");
    }

    private static string Arguments(
        bool withCoverage,
        string? testFilter = null,
        IEnumerable<string>? additional = null,
        IEnumerable<int>? ignoredExitCodes = null) =>
        ITestTarget.TestArguments(
            "C:/repo/Some.slnx",
            "Release",
            "C:/repo/artifacts/test-results",
            withCoverage,
            testFilter,
            additional,
            ignoredExitCodes);

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
}
