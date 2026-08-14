using FEx.Building;
using Nuke.Common.IO;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The flags that decide what this gate IS, and the decision it makes on the report it gets back. Each
/// assertion here stands for a way the gate can be defused while every other test stays green.
/// </summary>
public sealed class InspectTargetTests
{
    private static readonly AbsolutePath Solution = (AbsolutePath)"/repo/My Solution.slnx";
    private static readonly AbsolutePath Report = (AbsolutePath)"/repo/artifacts/inspection/inspectcode.sarif";
    private static readonly AbsolutePath Caches = (AbsolutePath)"/repo/.nuke/temp/inspectcode-caches";

    private static string Arguments(string severity = "ERROR") =>
        IInspectTarget.InspectionArguments(Solution, Report, Caches, Configuration.Release, severity);

    [Fact]
    public void TheReportPath_IsAlwaysAskedFor()
    {
        // Not an option: left out, the tool does not fall back to stdout - it dies inside its own logger
        // and reports a LoggerException instead of running.
        Arguments().ShouldContain("-o=");
    }

    [Fact]
    public void SolutionWideAnalysis_IsForcedOn()
    {
        // Measured on this repository's consumer: with it off the same solution reports four findings the
        // full run does not. The two modes are different gates, not a cheap and an expensive one.
        Arguments().ShouldContain("--swea");
    }

    [Fact]
    public void TheBuild_IsNotRepeated()
    {
        // The target depends on the compile step, so building again is the same work twice.
        Arguments().ShouldContain("--no-build");
    }

    [Fact]
    public void TheConfiguration_TravelsWithTheSkippedBuild()
    {
        // Without it the tool evaluates MSBuild at its own default and looks for a Debug build that a
        // release run never produced - reporting the whole solution as unresolvable rather than clean.
        Arguments().ShouldContain("--properties:Configuration=Release");
    }

    [Fact]
    public void TheSeverity_IsTheOneTheRepositoryDeclares()
    {
        Arguments("WARNING").ShouldContain("-e=WARNING");
    }

    [Fact]
    public void EveryPath_IsQuoted()
    {
        // A repository checked out under a path with a space is ordinary, and an unquoted path there turns
        // into two arguments - the tool then inspects something that does not exist, or nothing at all.
        Arguments().ShouldContain("\"/repo/My Solution.slnx\"");
        Arguments().ShouldContain($"-o=\"{Report}\"");
        Arguments().ShouldContain($"--caches-home=\"{Caches}\"");
    }

    [Fact]
    public void TheToolIsInvokedAsTheRepositorysOwnLocalTool()
    {
        // `dotnet jb` resolves the version pinned in .config/dotnet-tools.json. A gate that installed its
        // own copy would disagree with the developer's command the moment either was bumped.
        Arguments().ShouldStartWith("jb inspectcode ");
    }

    [Fact]
    public void OneFinding_FailsTheBuild()
    {
        // The whole point, and the one break that leaves every other test here green: a gate that reports
        // findings and returns anyway is a report, not a gate.
        var findings = new List<InspectionFinding> { new("Rule", "error", "src/Thing.cs", 42, "wrong") };

        Should.Throw<InvalidOperationException>(() => IInspectTarget.Verdict(findings))
            .Message.ShouldContain("1 finding");
    }

    [Fact]
    public void NoFindings_PassesQuietly()
    {
        Should.NotThrow(() => IInspectTarget.Verdict([]));
    }
}
