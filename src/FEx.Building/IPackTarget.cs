using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

public interface IPackTarget : ICompileTarget, IGitVersionComponent
{
    string? PackProject => null;

    // Projects (or solutions) to pack, one DotNetPack invocation each. Default: the whole solution
    // (PackProject override, else Solution.Path). Consumers that must pack a subset - e.g. a repo whose
    // solution also contains submodule projects it consumes as packages, not publishes - override this
    // to return only their own projects.
    IEnumerable<string> PackProjects => [PackProject ?? Solution.Path!];

    sealed AbsolutePath PackagesDirectory => RootDirectory / "artifacts" / "packages";

    Target Pack =>
        _ => _.Description("Creates NuGet packages with GitVersion-derived version")
            .DependsOn(Compile)
            .Produces(PackagesDirectory / "*.nupkg")
            .Executes(() =>
            {
                PackagesDirectory.CreateOrCleanDirectory();

                var version = NuGetVersion;

                Log.Information("Packing with version: {Version}", version);

                foreach (var project in PackProjects)
                {
                    DotNetPack(s => s.SetProject(project)
                        .SetConfiguration(Configuration)
                        .EnableNoBuild()
                        .SetOutputDirectory(PackagesDirectory)
                        .SetVersion(version)
                        .SetAssemblyVersion(VersionInfo!.AssemblySemVer)
                        .SetFileVersion(VersionInfo!.AssemblySemFileVer)
                        .SetInformationalVersion(InformationalVersion)
                        .SetProperty("PackageVersion", version)
                        .SetProperty("NoWarn", "CS1591"));
                }

                var packages = PackagesDirectory.GlobFiles("*.nupkg");
                Log.Information("Created {Count} package(s):", packages.Count);

                foreach (var pkg in packages)
                    Log.Information("  {Package}", pkg.Name);
            });
}