using System;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;

using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

public interface INuGetPublishTarget : IPackTarget
{
    [Parameter("NuGet source URL for pushing packages")]
    string NuGetSource => TryGetValue(() => NuGetSource)
                          ?? $"{Environment.GetEnvironmentVariable("CI_API_V4_URL")}/projects/{Environment.GetEnvironmentVariable("CI_PROJECT_ID")}/packages/nuget/index.json";

    [Parameter("NuGet API key for pushing packages")]
    [Secret]
    string? NuGetApiKey => TryGetValue(() => NuGetApiKey)
                           ?? Environment.GetEnvironmentVariable("CI_JOB_TOKEN");

    Target Publish => _ => _
        .Description("Publishes NuGet packages to the configured feed")
        .DependsOn(Pack)
        .OnlyWhenDynamic(() => !string.IsNullOrEmpty(NuGetApiKey),
            "Skipping publish: no NuGetApiKey / CI_JOB_TOKEN configured")
        .Executes(() =>
        {
            var packages = PackagesDirectory.GlobFiles("*.nupkg");

            if (packages.Count == 0)
            {
                Log.Warning("No .nupkg files found in {Dir}", PackagesDirectory);
                return;
            }

            Log.Information("Pushing {Count} package(s) to {Source}", packages.Count, NuGetSource);
            Log.Information("NuGetApiKey is {Status}", string.IsNullOrEmpty(NuGetApiKey) ? "EMPTY" : $"set ({NuGetApiKey!.Length} chars)");

            var failed = 0;

            foreach (var package in packages)
            {
                Log.Information("  Pushing {Package}", package.Name);

                try
                {
                    DotNetNuGetPush(s => s
                        .SetTargetPath(package)
                        .SetSource(NuGetSource)
                        .SetApiKey(NuGetApiKey!)
                        .SetProcessLogOutput(true));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to push {Package}: {Message}", package.Name, ex.Message);
                    failed++;
                    throw;
                }
            }

            Log.Information("Successfully pushed {Count} package(s) to {Source}", packages.Count, NuGetSource);
        });
}
