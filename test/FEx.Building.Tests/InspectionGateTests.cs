using FEx.Building;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The report is the verdict, because the tool's exit code is not one - <c>inspectcode</c> exits 0 whether
/// it found a hundred errors or none. These pin the reading of it, and the defects they guard against are
/// the ones that would leave a full report looking empty.
/// </summary>
public sealed class InspectionGateTests
{
    private const string OneError = """
        {
          "runs": [
            {
              "tool": { "driver": { "rules": [ { "id": "CheckNamespace",
                "defaultConfiguration": { "level": "error" } } ] } },
              "results": [
                {
                  "ruleId": "CheckNamespace",
                  "message": { "text": "Namespace does not correspond to file location" },
                  "locations": [
                    { "physicalLocation": {
                        "artifactLocation": { "uri": "src/Thing.cs" },
                        "region": { "startLine": 42 } } }
                  ]
                }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void Analyze_ReadsWhereTheFindingIs()
    {
        var findings = InspectionGate.Analyze(OneError);

        var finding = findings.ShouldHaveSingleItem();
        finding.RuleId.ShouldBe("CheckNamespace");
        finding.File.ShouldBe("src/Thing.cs");
        finding.Line.ShouldBe(42);
        finding.Message.ShouldBe("Namespace does not correspond to file location");
    }

    [Fact]
    public void Analyze_TakesTheLevelFromTheRule_WhenTheResultStatesNone()
    {
        // The usual shape for a promoted inspection: severity is declared once on the rule, and every
        // result inherits it. Reading only the result would call every finding level-less.
        InspectionGate.Analyze(OneError).ShouldHaveSingleItem().Level.ShouldBe("error");
    }

    [Fact]
    public void Analyze_PrefersTheLevelTheResultStatesItself()
    {
        var sarif = OneError.Replace("\"ruleId\": \"CheckNamespace\",",
            "\"ruleId\": \"CheckNamespace\", \"level\": \"warning\",", System.StringComparison.Ordinal);

        InspectionGate.Analyze(sarif).ShouldHaveSingleItem().Level.ShouldBe("warning");
    }

    [Fact]
    public void Analyze_SurvivesAByteOrderMark()
    {
        // The tool writes one often enough that a reader which cannot take it would fail on the report of
        // a clean solution - a gate that dies rather than passes, which reads exactly like a broken build.
        InspectionGate.Analyze('\uFEFF' + OneError).ShouldHaveSingleItem();
    }

    [Fact]
    public void Analyze_ReportsNothing_WhenTheRunFoundNothing()
    {
        InspectionGate.Analyze("""{ "runs": [ { "tool": { "driver": {} }, "results": [] } ] }""").ShouldBeEmpty();
    }

    [Fact]
    public void Analyze_ReportsNothing_WhenTheReportCarriesNoRunsAtAll()
    {
        InspectionGate.Analyze("{}").ShouldBeEmpty();
    }

    [Fact]
    public void Analyze_KeepsAFindingThatNamesNoFile()
    {
        // A solution-wide finding carries no location. Dropping it would be the gate quietly narrowing
        // itself to the findings that happen to have a line number.
        var sarif = """
            { "runs": [ { "tool": { "driver": { "rules": [] } },
              "results": [ { "ruleId": "Wide", "message": { "text": "no place" } } ] } ] }
            """;

        var finding = InspectionGate.Analyze(sarif).ShouldHaveSingleItem();
        finding.File.ShouldBeEmpty();
        finding.Line.ShouldBe(0);
        finding.ToString().ShouldStartWith("<solution>");
    }

    [Fact]
    public void Analyze_SaysSoWhenHandedSomethingThatIsNotTheReport()
    {
        // The report is SARIF JSON whatever extension it carries. Reading it as XML is the other half of
        // the same mistake, and it has to fail loudly rather than come back with no findings.
        Should.Throw<JsonException>(() => InspectionGate.Analyze("<report><issue /></report>"));
    }
}
