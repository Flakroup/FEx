using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

public interface INuGetPublishTarget : IPackTarget
{
    [Parameter("NuGet source URL for pushing packages (default: nuget.org)")]
    string NuGetSource => TryGetValue(() => NuGetSource) ?? "https://api.nuget.org/v3/index.json";

    [Parameter("NuGet API key for pushing packages")]
    // FExBuild.MaskSecrets keeps this out of the build log - both the parameter listing and the command
    // line echo, matched by value so the option's spelling does not matter.
    [Secret]
    string? NuGetApiKey => TryGetValue(() => NuGetApiKey);

    Target Publish =>
        _ => _.Description("Publishes NuGet packages to the configured feed")
            .DependsOn(Pack)
            .OnlyWhenDynamic(() => !string.IsNullOrEmpty(NuGetApiKey), "Skipping publish: no NuGetApiKey configured")
            // A commit that COMPLETED a publish carries the version tag its last step pushed, so a re-run
            // ships nothing rather than the same sources again under a fresh version. A run that died
            // part-way left no tag, and that case is deliberately not skipped here - re-running it is how
            // the remaining packages reach the feed. See PushSettings for what makes that re-run work.
            .OnlyWhenDynamic(() => !GitTags.MarksReleasedCommit(GitTags.OnHead(TagPrefix)),
                "Skipping publish: HEAD already carries a version tag, so this commit was already published")
            .Executes(() =>
            {
                var packages = PackagesDirectory.GlobFiles("*.nupkg");

                if (packages.Count == 0)
                {
                    Log.Warning("No .nupkg files found in {Dir}", PackagesDirectory);

                    return;
                }

                Log.Information("Pushing {Count} package(s) to {Source}", packages.Count, NuGetSource);

                foreach (var package in packages)
                {
                    Log.Information("  Pushing {Package}", package.Name);

                    DotNetNuGetPush(s => PushSettings(s, package, NuGetSource, NuGetApiKey!));
                }

                Log.Information("Successfully pushed {Count} package(s) to {Source}", packages.Count, NuGetSource);
            });

    /// <summary>
    /// How one package is pushed: where it goes, what authorises it, and that a version already on the feed
    /// is a no-op rather than an error.
    /// </summary>
    /// <remarks>
    /// <c>--skip-duplicate</c> is what makes a publish RESUMABLE, and the resumability is the point. The
    /// packages go up one at a time, so a run that dies part-way - a lost runner, a network drop - leaves
    /// the feed holding some of them and the rest nowhere. Without the flag the re-run that would finish
    /// the job fails on the first package already there, because <c>dotnet nuget push</c> treats an
    /// existing version as an error, and the missing packages never ship at all. The tag guard on the
    /// target cannot cover this: the tag is pushed after the last package, so a run that never finished
    /// never left one.
    /// <para>
    /// Measured, on the run that prompted this: 53 of the packages reached nuget.org, 6 did not, and every
    /// published one declared a dependency on a version of the missing ones that was not there - a set no
    /// consumer could restore.
    /// </para>
    /// <para>
    /// Extracted from the target body so the composition is reachable from a test; the target's own call to
    /// it is not, because exercising a NUKE target needs NUKE.
    /// </para>
    /// </remarks>
    static DotNetNuGetPushSettings PushSettings(DotNetNuGetPushSettings settings,
                                                string package,
                                                string source,
                                                string apiKey) =>
        settings.SetTargetPath(package).SetSource(source).SetApiKey(apiKey).EnableSkipDuplicate();
}