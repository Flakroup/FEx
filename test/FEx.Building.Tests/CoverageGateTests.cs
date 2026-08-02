using Shouldly;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The gate is what stops untested code shipping, so its own arithmetic is pinned here: union across
/// reports, generated code ignored, exclusions honoured, and an absent assembly failing loudly instead
/// of passing on zero measurable lines.
/// </summary>
public sealed class CoverageGateTests
{
    private const string Root = @"X:\repo";

    [Fact]
    public void FullyCoveredFile_Passes()
    {
        CoverageReport report = Analyze(
            Options(),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 3)));

        report.Failed.ShouldBeFalse();
        report.IncompleteFiles.ShouldBeEmpty();
        report.Rate.ShouldBe(1d);
    }

    [Fact]
    public void UncoveredLine_FailsAndIsListedByNumber()
    {
        CoverageReport report = Analyze(
            Options(),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 0), (7, 0)));

        report.Failed.ShouldBeTrue();
        CoverageFile file = report.IncompleteFiles.ShouldHaveSingleItem();
        file.Path.ShouldBe("src/Acme.Core/Money.cs");
        file.UncoveredLines.ShouldBe([2, 7]);
    }

    [Fact]
    public void TwoReportsCoveringDifferentLines_AreMergedAsUnion_NotAveraged()
    {
        // The same class exercised by two test projects: neither report alone is complete, together
        // they are. Averaging the per-report rates would report ~50% and fail a fully covered file.
        CoverageReport report = Analyze(
            Options(),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 0)),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 0), (2, 5)));

        report.Failed.ShouldBeFalse();
        report.CoveredLines.ShouldBe(2);
        report.MeasurableLines.ShouldBe(2);
    }

    [Fact]
    public void GeneratedCodeUnderObj_IsIgnored()
    {
        CoverageReport report = Analyze(
            Options(),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\obj\Release\net10.0\Regex.g.cs", (1, 0), (2, 0)));

        report.Failed.ShouldBeFalse();
        report.Files.ShouldBeEmpty();
    }

    [Fact]
    public void ExcludedFile_DoesNotFailTheGate()
    {
        CoverageReport report = Analyze(
            Options(exclusions: ["src/Acme.Core/Migrations"]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Migrations\Initial.cs", (1, 0)));

        report.Failed.ShouldBeFalse();
        report.Files.ShouldBeEmpty();
    }

    [Fact]
    public void ExclusionOfADirectory_DoesNotSwallowASiblingWithTheSamePrefix()
    {
        // "Migrations" must not also exempt "MigrationsHelper.cs" - a prefix match on the raw string
        // would quietly widen every exclusion.
        CoverageReport report = Analyze(
            Options(exclusions: ["src/Acme.Core/Migrations"]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\MigrationsHelper.cs", (1, 0)));

        report.Failed.ShouldBeTrue();
        report.IncompleteFiles.ShouldHaveSingleItem().Path.ShouldBe("src/Acme.Core/MigrationsHelper.cs");
    }

    [Fact]
    public void MarkedLine_DoesNotFailTheGate_ButItsUncoveredSiblingsStillDo()
    {
        CoverageReport report = Analyze(
            Options(source: ["covered", "throw; // coverage-exclude: unreachable guard", "", "", "", "", "forgotten"]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 0), (7, 0)));

        report.Failed.ShouldBeTrue();
        CoverageFile file = report.IncompleteFiles.ShouldHaveSingleItem();
        file.UncoveredLines.ShouldBe([7]); // line 2 marked, line 7 still fails the gate
    }

    [Fact]
    public void MarkedLine_RemovesItFromBothTheNumeratorAndDenominator()
    {
        // Exempting a line must not count it as "covered" (which would inflate the rate) - it is removed
        // from the measurable total entirely, as if it were never instrumented.
        CoverageReport report = Analyze(
            Options(source: ["covered", "throw; // coverage-exclude: unreachable guard"]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 0)));

        report.Failed.ShouldBeFalse();
        report.MeasurableLines.ShouldBe(1);
        report.CoveredLines.ShouldBe(1);
        report.Rate.ShouldBe(1d);
    }

    /// <summary>
    /// The failure this whole mechanism exists to stop. A marker on a covered line excuses nothing, and
    /// worse, would go on hiding that line if a real gap appeared there later - so it fails the gate
    /// instead of sitting quietly, which is what a line-number entry used to do after any edit above it.
    /// </summary>
    [Fact]
    public void MarkerOnACoveredLine_FailsTheGate_NamingTheLine()
    {
        CoverageReport report = Analyze(
            Options(source: ["covered", "also covered // coverage-exclude: stale, this got tested"]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 1)));

        report.Failed.ShouldBeTrue();
        report.IncompleteFiles.ShouldBeEmpty();
        StaleExclusion stale = report.StaleExclusions.ShouldHaveSingleItem();
        stale.Path.ShouldBe("src/Acme.Core/Money.cs");
        stale.Line.ShouldBe(2);
        stale.Reason.ShouldBe(StaleReason.LineIsCovered);
    }

    /// <summary>
    /// A marker on a line this run did not instrument is REPORTED, not fatal. Whether a closing brace or a
    /// bare `continue` gets a sequence point differs between Debug and Release and between platforms, so the
    /// same marker is genuinely needed where a developer runs the gate and genuinely inert on CI. Failing on
    /// it would mean no marker could satisfy both at once - measured, not assumed: markers that were
    /// required on Windows/Debug came back as "nothing to exclude" on Linux/Release.
    /// </summary>
    [Fact]
    public void MarkerOnALineWithNoInstrumentedCode_IsReportedButDoesNotFailTheGate()
    {
        CoverageReport report = Analyze(
            Options(source: ["covered", "} // coverage-exclude: a brace this configuration does not instrument"]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1)));

        report.Failed.ShouldBeFalse();
        report.StaleExclusions.ShouldBeEmpty();
        StaleExclusion unused = report.UnusedExclusions.ShouldHaveSingleItem();
        unused.Line.ShouldBe(2);
        unused.Reason.ShouldBe(StaleReason.NothingToExclude);
    }

    /// <summary>The distinction that keeps the gate useful: a marker on an UNINSTRUMENTED line is a
    /// configuration difference, a marker on a COVERED line is rot - only the second one fails.</summary>
    [Fact]
    public void ACoveredLineStillFails_EvenWhileAnUninstrumentedOneOnlyReports()
    {
        CoverageReport report = Analyze(
            Options(source:
            [
                "covered",
                "also covered // coverage-exclude: rot - this got tested",
                "} // coverage-exclude: a brace this configuration does not instrument",
            ]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 1)));

        report.Failed.ShouldBeTrue();
        report.StaleExclusions.ShouldHaveSingleItem().Line.ShouldBe(2);
        report.UnusedExclusions.ShouldHaveSingleItem().Line.ShouldBe(3);
    }

    /// <summary>An exemption nobody can review is not an exemption. The reason is the entire reason the
    /// marker is allowed to exist at all.</summary>
    [Fact]
    public void MarkerWithNoReason_FailsTheGate_EvenOnAGenuinelyUncoveredLine()
    {
        CoverageReport report = Analyze(
            Options(source: ["covered", "throw; // coverage-exclude:"]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 0)));

        report.Failed.ShouldBeTrue();
        report.StaleExclusions.ShouldHaveSingleItem().Reason.ShouldBe(StaleReason.NoReasonGiven);
    }

    /// <summary>
    /// A statement split across lines cannot carry a comment on its continuations - they are inside a
    /// string literal - so the marker states its span outright. Every line of that span is still judged
    /// separately, which is what stops a span from quietly swallowing the code below it.
    /// </summary>
    [Fact]
    public void MarkerWithASpan_CoversTheContinuationLinesToo()
    {
        CoverageReport report = Analyze(
            Options(source:
            [
                "covered",
                "throw new Exception( // coverage-exclude+2: unreachable, the caller checked already",
                "    $\"first half {value}\"",
                "    + \"second half\");",
            ]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 0), (3, 0), (4, 0)));

        report.Failed.ShouldBeFalse();
        report.MeasurableLines.ShouldBe(1);
    }

    [Fact]
    public void ASpanReachingPastWhatItExcuses_FailsTheGate()
    {
        CoverageReport report = Analyze(
            Options(source: ["covered", "throw; // coverage-exclude+1: reaches one line too far", "covered too"]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 0), (3, 1)));

        report.Failed.ShouldBeTrue();
        StaleExclusion stale = report.StaleExclusions.ShouldHaveSingleItem();
        stale.Line.ShouldBe(3);
        stale.Reason.ShouldBe(StaleReason.LineIsCovered);
    }

    /// <summary>A marker written without its colon is reported, not ignored: silently doing nothing is the
    /// exact behaviour this mechanism replaces.</summary>
    [Fact]
    public void MarkerWithoutItsColon_IsReportedRatherThanIgnored()
    {
        CoverageReport report = Analyze(
            Options(source: ["covered", "throw; // coverage-exclude unreachable guard"]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 0)));

        report.Failed.ShouldBeTrue();
        report.StaleExclusions.ShouldHaveSingleItem().Reason.ShouldBe(StaleReason.NoReasonGiven);
    }

    /// <summary>Source the checkout does not have (a submodule built elsewhere) says nothing about
    /// coverage, so it is not a verdict - the file is policed on its report alone.</summary>
    [Fact]
    public void SourceThatCannotBeRead_LeavesTheFileJudgedOnItsReport()
    {
        CoverageReport report = Analyze(
            Options(source: null),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 0)));

        report.StaleExclusions.ShouldBeEmpty();
        report.IncompleteFiles.ShouldHaveSingleItem().UncoveredLines.ShouldBe([2]);
    }

    /// <summary>With no reader wired in, Analyze touches no filesystem at all - the property that keeps it
    /// a pure function and lets every test above run without one.</summary>
    [Fact]
    public void WithNoSourceReader_NoMarkerApplies()
    {
        CoverageReport report = CoverageGate.Analyze(
            [Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 0))],
            new CoverageGateOptions { RootDirectory = Root, IncludedPrefixes = ["src"] });

        report.StaleExclusions.ShouldBeEmpty();
        report.IncompleteFiles.ShouldHaveSingleItem().UncoveredLines.ShouldBe([1]);
    }

    [Fact]
    public void FileOutsideTheIncludedPrefix_IsNotPoliced()
    {
        CoverageReport report = Analyze(
            Options(),
            Cobertura("Acme.Tests", @"X:\repo\test\Acme.Tests\MoneyTests.cs", (1, 0)));

        report.Failed.ShouldBeFalse();
        report.Files.ShouldBeEmpty();
    }

    [Fact]
    public void ExpectedModuleAbsentFromEveryReport_FailsInsteadOfPassingVacuously()
    {
        // The P6d failure mode: an assembly nothing loads contributes no measurable lines, so a naive
        // gate scores it 0/0 = 100% and waves it through.
        CoverageReport report = Analyze(
            Options(expectedModules: ["Acme.Core", "Acme.Untested"]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1)));

        report.Failed.ShouldBeTrue();
        report.MissingModules.ShouldBe(["Acme.Untested"]);
        report.IncompleteFiles.ShouldBeEmpty();
    }

    [Fact]
    public void AllExpectedModulesPresent_ReportsNoneMissing()
    {
        CoverageReport report = Analyze(
            Options(expectedModules: ["Acme.Core"]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1)));

        report.MissingModules.ShouldBeEmpty();
        report.Modules.ShouldBe(["Acme.Core"]);
        report.Failed.ShouldBeFalse();
    }

    [Fact]
    public void LineRepeatedUnderMethodAndClass_IsCountedOnce()
    {
        // Cobertura lists each line twice: once under <method>, once under the class-level <lines>.
        XDocument report = XDocument.Parse(
            $"""
             <coverage>
               <packages>
                 <package name="Acme.Core">
                   <classes>
                     <class filename="{Root}\src\Acme.Core\Money.cs">
                       <methods>
                         <method>
                           <lines><line number="1" hits="1" /></lines>
                         </method>
                       </methods>
                       <lines><line number="1" hits="1" /></lines>
                     </class>
                   </classes>
                 </package>
               </packages>
             </coverage>
             """);

        CoverageReport result = Analyze(Options(), report);

        result.MeasurableLines.ShouldBe(1);
        result.CoveredLines.ShouldBe(1);
    }

    [Fact]
    public void PathOutsideTheRepositoryRoot_IsSkipped()
    {
        CoverageReport report = Analyze(
            Options(),
            Cobertura("Acme.Core", @"C:\elsewhere\src\Other.cs", (1, 0)));

        report.Files.ShouldBeEmpty();
        report.Failed.ShouldBeFalse();
    }

    [Fact]
    public void NoReports_YieldNothingToPolice()
    {
        CoverageReport report = CoverageGate.Analyze([], Options());

        report.Files.ShouldBeEmpty();
        report.Rate.ShouldBe(1d);
        report.Failed.ShouldBeFalse();
    }

    private static CoverageReport Analyze(CoverageGateOptions options, params XDocument[] reports) =>
        CoverageGate.Analyze(reports, options);

    /// <summary>
    /// Options for one file's worth of source. <paramref name="source" /> is what the reader hands back for
    /// EVERY path - the tests each use a single file, and a null stands for source this checkout cannot
    /// read. The reader is a delegate precisely so these tests never touch a disk.
    /// </summary>
    private static CoverageGateOptions Options(
        string[]? exclusions = null, string[]? expectedModules = null, string[]? source = null) =>
        new()
        {
            RootDirectory = Root,
            IncludedPrefixes = ["src"],
            Exclusions = exclusions ?? [],
            ExpectedModules = expectedModules ?? [],
            ReadSourceLines = _ => source,
        };

    /// <summary>Builds a minimal cobertura document: one package, one class, the given (line, hits) pairs.</summary>
    private static XDocument Cobertura(string module, string filename, params (int Line, int Hits)[] lines)
    {
        string body = string.Join("", lines.Select(l => $"""<line number="{l.Line}" hits="{l.Hits}" />"""));

        return XDocument.Parse(
            $"""
             <coverage>
               <packages>
                 <package name="{module}">
                   <classes>
                     <class filename="{filename}">
                       <lines>{body}</lines>
                     </class>
                   </classes>
                 </package>
               </packages>
             </coverage>
             """);
    }
}
