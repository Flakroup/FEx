using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;

using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

public interface IPackTarget : IGitVersionComponent
{
    sealed AbsolutePath PackagesDirectory => RootDirectory / "artifacts" / "packages";

    [Parameter("Solution file path to pack")]
    string PackSolution => TryGetValue(() => PackSolution)
                           ?? NukeBuild.RootDirectory.GlobFiles("*.slnx", "*.sln").FirstOrDefault()?.ToString()
                           ?? throw new InvalidOperationException("No solution file found. Set --pack-solution or add a .slnx/.sln to the root.");

    Target Pack => _ => _
        .Description("Creates NuGet packages with GitVersion-derived version")
        .Produces(PackagesDirectory / "*.nupkg")
        .Executes(() =>
        {
            PackagesDirectory.CreateOrCleanDirectory();

            var version = NuGetVersion;

            Log.Information("Packing with version: {Version}", version);

            DotNetPack(s => s
                .SetProject(PackSolution)
                .SetConfiguration("Release")
                .SetOutputDirectory(PackagesDirectory)
                .SetVersion(version)
                .SetAssemblyVersion(VersionInfo!.AssemblySemVer)
                .SetFileVersion(VersionInfo!.AssemblySemFileVer)
                .SetInformationalVersion(InformationalVersion)
                .SetProperty("PackageVersion", version)
                .SetProperty("NoWarn", "CS1591")
                .SetProperty("NuGetAudit", !NukeBuild.IsServerBuild));

            var packages = PackagesDirectory.GlobFiles("*.nupkg");
            Log.Information("Created {Count} package(s):", packages.Count);

            foreach (var pkg in packages)
                Log.Information("  {Package}", pkg.Name);
        });
}
