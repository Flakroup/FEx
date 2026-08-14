using FEx.Building;
using Shouldly;
using System;
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
              "invocations": [ { "executionSuccessful": true } ],
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
    public void Analyze_SurvivesAByteOrderMark()
    {
        // The tool writes one often enough that a reader which cannot take it would fail on the report of
        // a clean solution - a gate that dies rather than passes, which reads exactly like a broken build.
        InspectionGate.Analyze('\uFEFF' + OneError).ShouldHaveSingleItem();
    }

    [Fact]
    public void Analyze_ReportsNothing_WhenTheRunFoundNothing()
    {
        InspectionGate.Analyze(
            """
            { "runs": [ { "invocations": [ { "executionSuccessful": true } ],
              "tool": { "driver": {} }, "results": [] } ] }
            """).ShouldBeEmpty();
    }

    [Fact]
    public void Analyze_RefusesAReportThatCarriesNoRun()
    {
        // An empty report is not a clean one. This assertion used to say the opposite, and composed with
        // the target's "no findings passes quietly" it proved that `{}` ships a green release gate.
        Should.Throw<InvalidOperationException>(() => InspectionGate.Analyze("{}"))
            .Message.ShouldContain("no run at all");
    }

    [Fact]
    public void Analyze_RefusesARunThatCannotSayItFinished()
    {
        // `inspectcode` exits 0 whether it inspected the solution or failed to load it, so the report has
        // to vouch for its own run before its emptiness means anything.
        var sarif = OneError.Replace("\"invocations\": [ { \"executionSuccessful\": true } ],", string.Empty,
            StringComparison.Ordinal);

        Should.Throw<InvalidOperationException>(() => InspectionGate.Analyze(sarif))
            .Message.ShouldContain("no invocation");
    }

    [Fact]
    public void Analyze_RefusesARunThatReportsItselfUnsuccessful()
    {
        var sarif = OneError.Replace("\"executionSuccessful\": true", "\"executionSuccessful\": false",
            StringComparison.Ordinal);

        Should.Throw<InvalidOperationException>(() => InspectionGate.Analyze(sarif))
            .Message.ShouldContain("proves nothing");
    }

    [Fact]
    public void Analyze_RefusesARunWhoseInvocationOmitsTheField()
    {
        // Absent is not true. A report that simply does not carry the field is one that does not vouch.
        var sarif = OneError.Replace("{ \"executionSuccessful\": true }", "{ }", StringComparison.Ordinal);

        Should.Throw<InvalidOperationException>(() => InspectionGate.Analyze(sarif))
            .Message.ShouldContain("proves nothing");
    }

    [Fact]
    public void Analyze_KeepsAFindingThatNamesNoFile()
    {
        // A solution-wide finding carries no location. Dropping it would be the gate quietly narrowing
        // itself to the findings that happen to have a line number.
        var sarif = """
            { "runs": [ { "invocations": [ { "executionSuccessful": true } ],
              "tool": { "driver": { "rules": [] } },
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
