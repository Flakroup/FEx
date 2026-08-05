using Shouldly;
using System;
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

    private static string Arguments(bool withCoverage) =>
        ITestTarget.TestArguments("C:/repo/Some.slnx", "Release", "C:/repo/artifacts/test-results", withCoverage);

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
