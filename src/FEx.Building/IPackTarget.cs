using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;

using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

public interface IPackTarget : ICompileTarget, IGitVersionComponent
{
    sealed AbsolutePath PackagesDirectory => RootDirectory / "artifacts" / "packages";

    Target Pack => _ => _
        .Description("Creates NuGet packages with GitVersion-derived version")
        .DependsOn(Compile)
        .Produces(PackagesDirectory / "*.nupkg")
        .Executes(() =>
        {
            PackagesDirectory.CreateOrCleanDirectory();

            var version = NuGetVersion;

            Log.Information("Packing with version: {Version}", version);

            DotNetPack(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
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
