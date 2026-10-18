using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
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

    // The version Pack stamped on the packages, written beside them. A later job that tags the release
    // reads it instead of resolving GitVersion again, so the tag cannot name a version no package carries.
    sealed AbsolutePath ReleaseVersionFile => PackagesDirectory / "release-version.txt";

    // Settings for one DotNetPack of a project. Extracted from the target body, which needs NUKE to run, so
    // a test reaches the settings - the version is passed in because resolving it launches GitVersion.
    virtual DotNetPackSettings GetPackSettings(DotNetPackSettings s,
                                               string project,
                                               string version,
                                               GitVersionInfo versionInfo,
                                               string informationalVersion) =>
        s.SetProject(project)
            .SetConfiguration(Configuration)
            .EnableNoBuild()
            .SetOutputDirectory(PackagesDirectory)
            .SetVersion(version)
            .SetAssemblyVersion(versionInfo.AssemblySemVer)
            .SetFileVersion(versionInfo.AssemblySemFileVer)
            .SetInformationalVersion(informationalVersion)
            .SetProperty("PackageVersion", version)
            .SetProperty("NoWarn", "CS1591")
            .AddProcessAdditionalArguments(Parallelism.MsBuildArguments(Parallelism.Resolve(MaxParallelism)));

    Target Pack =>
        _ => _.Description("Creates NuGet packages with GitVersion-derived version")
            .DependsOn(Compile)
            .Produces(PackagesDirectory / "*.nupkg", PackagesDirectory / "*.snupkg")
            .Executes(() =>
            {
                PackagesDirectory.CreateOrCleanDirectory();

                var version = NuGetVersion;

                Log.Information("Packing with version: {Version}", version);

                foreach (var project in PackProjects)
                {
                    DotNetPack(s => GetPackSettings(s, project, version, VersionInfo!, InformationalVersion));
                }

                ReleaseVersionFile.WriteAllText(version);

                var packages = PackagesDirectory.GlobFiles("*.nupkg");
                Log.Information("Created {Count} package(s):", packages.Count);

                foreach (var pkg in packages)
                    Log.Information("  {Package}", pkg.Name);
            });
}