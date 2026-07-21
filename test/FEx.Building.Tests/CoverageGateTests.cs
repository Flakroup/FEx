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
    public void ExcludedLine_DoesNotFailTheGate_ButItsUncoveredSiblingsStillDo()
    {
        CoverageReport report = Analyze(
            Options(lineExclusions: [("src/Acme.Core/Money.cs", 2)]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 0), (7, 0)));

        report.Failed.ShouldBeTrue();
        CoverageFile file = report.IncompleteFiles.ShouldHaveSingleItem();
        file.UncoveredLines.ShouldBe([7]); // line 2 excluded, line 7 still fails the gate
    }

    [Fact]
    public void ExcludedLine_RemovesItFromBothTheNumeratorAndDenominator()
    {
        // Excluding a line must not count it as "covered" (which would inflate the rate) - it is removed
        // from the measurable total entirely, as if it were never instrumented.
        CoverageReport report = Analyze(
            Options(lineExclusions: [("src/Acme.Core/Money.cs", 2)]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 0)));

        report.Failed.ShouldBeFalse();
        report.MeasurableLines.ShouldBe(1);
        report.CoveredLines.ShouldBe(1);
        report.Rate.ShouldBe(1d);
    }

    [Fact]
    public void LineExclusionThatMatchesNothing_IsANoOp()
    {
        // A stale entry (the line got covered by a later test, or the file was edited and renumbered)
        // must not silently exclude some OTHER, unrelated line - it excludes nothing, and the file's real
        // uncovered lines still fail the gate exactly as if the entry were absent.
        CoverageReport report = Analyze(
            Options(lineExclusions: [("src/Acme.Core/Money.cs", 99)]),
            Cobertura("Acme.Core", @"X:\repo\src\Acme.Core\Money.cs", (1, 1), (2, 0)));

        report.Failed.ShouldBeTrue();
        report.IncompleteFiles.ShouldHaveSingleItem().UncoveredLines.ShouldBe([2]);
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

    private static CoverageGateOptions Options(
        string[]? exclusions = null, string[]? expectedModules = null, (string Path, int Line)[]? lineExclusions = null) =>
        new()
        {
            RootDirectory = Root,
            IncludedPrefixes = ["src"],
            Exclusions = exclusions ?? [],
            LineExclusions = lineExclusions ?? [],
            ExpectedModules = expectedModules ?? []
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
