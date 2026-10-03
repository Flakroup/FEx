using Nuke.Common.Tools.DotNet;
using Shouldly;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The publish pushes the packages one at a time, so anything that interrupts it leaves the feed holding
/// some of them. These pin what lets a re-run finish that job instead of failing on the first package
/// already on the feed.
/// </summary>
public sealed class NuGetPublishTargetTests
{
    /// <summary>
    /// The one assertion this file exists for. Measured on a real run: the runner was lost mid-push, 53
    /// packages were on nuget.org and 6 were not, every published one declaring a dependency on a version
    /// of the missing ones that did not exist. Without this flag the re-run that would have shipped those
    /// 6 fails on the first of the 53, so the only way out is to abandon the version and publish another.
    /// </summary>
    [Fact]
    public void APackageAlreadyOnTheFeed_IsSkipped_SoAnInterruptedPublishIsFinishedByARerun() =>
        Settings().SkipDuplicate.ShouldBe(true);

    [Fact]
    public void ThePackage_TheFeedAndTheKey_ReachThePush()
    {
        var settings = Settings();

        settings.TargetPath.ShouldBe(Package);
        settings.Source.ShouldBe(Source);
        settings.ApiKey.ShouldBe(ApiKey);
    }

    /// <summary>
    /// A feed URL can embed its credential, and NUKE does not treat the source as a secret argument - so a
    /// failed push's ProcessException message repeated it in full (#190 review). The source is handed to NUKE's
    /// own redaction, so the echo and that message carry [REDACTED] before the log pipeline ever sees them.
    /// </summary>
    [Fact]
    public void TheSource_IsRedactedByNukeItself() =>
        Settings().ProcessRedactedSecrets.ShouldNotBeNull().ShouldContain(Source);

    private const string Package = "C:/repo/artifacts/packages/FEx.Core.0.3.0.nupkg";

    private const string Source = "https://api.nuget.org/v3/index.json";

    private const string ApiKey = "not-a-real-key";

    private static DotNetNuGetPushSettings Settings() =>
        INuGetPublishTarget.PushSettings(new DotNetNuGetPushSettings(), Package, Source, ApiKey);
}
