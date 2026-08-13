using Nuke.Common.IO;
using Shouldly;
using System.Collections.Generic;
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
        var settings = ((ICompileTarget)Build).GetBuildSettings(new(), Solution, noRestore);

        ShouldLeaveTheAuditAlone(settings.Properties);
    }

    [Fact]
    public void BuildSettings_KeepNoRestoreAndTheBinaryLog()
    {
        var settings = ((ICompileTarget)Build).GetBuildSettings(new(), Solution);

        settings.NoRestore.ShouldBe(true);
        settings.ProcessAdditionalArguments.ShouldBe(["-m", "-bl"]);
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
}