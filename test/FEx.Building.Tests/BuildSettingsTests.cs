using Nuke.Common.IO;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The dependency audit is a security gate, and the build must not be the thing that switches it off.
/// It used to be disabled on exactly the runs that matter - <c>NuGetAudit=!IsServerBuild</c> left it on
/// locally and off on CI - so a vulnerable transitive package reached a published artifact without any
/// build ever reporting it. These pin every settings path that reaches restore, build and pack.
/// </summary>
public sealed class BuildSettingsTests
{
    private static readonly TestBuild Build = new();

    private static AbsolutePath Solution => "/repo/FEx.slnx";

    [Fact]
    public void RestoreSettings_DoNotDisableTheAudit()
    {
        var settings = ((ICompileTarget)Build).GetRestoreSettings(new(), Solution);

        ShouldLeaveTheAuditAlone(settings.Properties);
    }

    [Fact]
    public void RestoreSettings_StillCarryTheConfigurationTheyAreGiven()
    {
        // The configuration override shares the chain the audit property was removed from.
        var settings = ((ICompileTarget)Build).GetRestoreSettings(new(), Solution, Configuration.Release);

        settings.Properties.ShouldContainKey("Configuration");
        settings.Properties["Configuration"].ToString().ShouldBe("Release");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildSettings_DoNotDisableTheAudit_WhicheverWayRestoreIsHandled(bool noRestore)
    {
        // The disable used to hang off `noRestore`, so both branches have to be pinned: with an implicit
        // restore, that branch set the property; without one, the restore step set it instead.
        var settings = ((ICompileTarget)Build).GetBuildSettings(new(), Solution, noRestore: noRestore);

        ShouldLeaveTheAuditAlone(settings.Properties);
    }

    [Fact]
    public void BuildSettings_AreUnboundedByDefault()
    {
        ((ICompileTarget)Build).CompileTimeout.ShouldBeNull();
        ((ICompileTarget)Build).GetBuildSettings(new(), Solution).ProcessExecutionTimeout.ShouldBeNull();
    }

    [Fact]
    public void BuildSettings_CarryTheCompileTimeout()
    {
        var build = new TimedBuild();

        ((ICompileTarget)build).GetBuildSettings(new(), Solution).ProcessExecutionTimeout
            .ShouldBe((int)TimeSpan.FromMinutes(7).TotalMilliseconds);
    }

    [Fact]
    public void BuildSettings_KeepNoRestoreAndTheBinaryLog()
    {
        var settings = ((ICompileTarget)Build).GetBuildSettings(new(), Solution);

        settings.NoRestore.ShouldBe(true);
        settings.ProcessAdditionalArguments.ShouldNotBeNull().ShouldContain("-m");
        settings.ProcessAdditionalArguments.ShouldContain(static a => a.StartsWith("-bl:") && a.EndsWith("FEx.slnx.binlog"));
    }

    /// <summary>
    /// A RID publish builds the solution and then every publish project again. With a bare <c>-bl</c> every
    /// one of those invocations wrote the same <c>msbuild.binlog</c>, so a green two-project publish kept only
    /// the last project's log (#56). Drives the real scope and the real settings, so it fails on either.
    /// </summary>
    [Fact]
    public void ARuntimePublishOfTwoProjects_WritesADistinctBinaryLogPerInvocation()
    {
        AbsolutePath app = "/repo/src/Sample.App/Sample.App.csproj";
        AbsolutePath worker = "/repo/src/Sample.Worker/Sample.Worker.csproj";

        var binlogs = ICompileTarget.Scope(Solution,
                runtimeSpecific: true,
                [new(app, (AbsolutePath)"/repo/artifacts/publish/a"), new(worker, (AbsolutePath)"/repo/artifacts/publish/w")])
            .Select(step => ((ICompileTarget)Build).GetBuildSettings(new(), step.Project, step.WithRuntime ? "linux-x64" : null))
            .Select(static s => s.ProcessAdditionalArguments!.Single(static a => a.StartsWith("-bl")))
            .ToList();

        binlogs.Count.ShouldBe(3);
        binlogs.Distinct().Count().ShouldBe(3, string.Join(", ", binlogs));
        binlogs.ShouldContain(static a => a.EndsWith("Sample.App.csproj.linux-x64.binlog"));
        binlogs.ShouldContain(static a => a.EndsWith("Sample.Worker.csproj.linux-x64.binlog"));
    }

    [Fact]
    public void BuildSettings_CarryTheRuntimeTheyAreGiven()
    {
        // The runtime moved into GetBuildSettings together with the binary log name that depends on it.
        ((ICompileTarget)Build).GetBuildSettings(new(), Solution, "linux-arm64").Runtime.ShouldBe("linux-arm64");
        ((ICompileTarget)Build).GetBuildSettings(new(), Solution).Runtime.ShouldBeNull();
    }

    // Properties is null - not an empty dictionary - until something sets one, so the check has to survive
    // that rather than dereference it.
    private static void ShouldLeaveTheAuditAlone(IReadOnlyDictionary<string, object>? properties) =>
        (properties?.ContainsKey("NuGetAudit") ?? false).ShouldBeFalse(
            "the build must never switch the dependency audit off");

    // FExBuild is abstract; nothing here touches build state, only the settings the two methods return.
    private sealed class TestBuild : FExBuild
    {
        public override IEnumerable<string> PublishProjects { get; } = [];
    }

    private sealed class TimedBuild : FExBuild, ICompileTarget
    {
        public override IEnumerable<string> PublishProjects { get; } = [];

        public TimeSpan? CompileTimeout => TimeSpan.FromMinutes(7);
    }
}
