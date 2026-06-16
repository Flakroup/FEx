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
    [Secret]
    string? NuGetApiKey => TryGetValue(() => NuGetApiKey);

    Target Publish =>
        _ => _.Description("Publishes NuGet packages to the configured feed")
            .DependsOn(Pack)
            .OnlyWhenDynamic(() => !string.IsNullOrEmpty(NuGetApiKey), "Skipping publish: no NuGetApiKey configured")
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

                    DotNetNuGetPush(s => s.SetTargetPath(package).SetSource(NuGetSource).SetApiKey(NuGetApiKey!));
                }

                Log.Information("Successfully pushed {Count} package(s) to {Source}", packages.Count, NuGetSource);
            });
}