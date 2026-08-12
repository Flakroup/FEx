using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

/// <summary>
/// Publishes deployable applications. Distinct from <see cref="INuGetPublishTarget" />, which pushes
/// packages to a feed - hence <c>PublishApp</c> rather than <c>Publish</c>, so a repo can implement both.
/// <para>
/// This exists because <c>dotnet build</c> and <c>dotnet test</c> never exercise the publish pipeline, and
/// for anything with a Blazor WebAssembly client that pipeline is where the real work happens: assembly
/// linking, the <c>blazor.boot.json</c> manifest, compression, static asset fingerprinting. A repo whose CI
/// only builds and tests can be green while its deployable output is broken - the failure then surfaces in
/// the container build, after the merge.
/// </para>
/// </summary>
public interface IAppPublishTarget : ICompileTarget
{
    /// <summary>Projects to publish, one <c>dotnet publish</c> each. Empty by default: publishing is
    /// opt-in, and "the whole solution" is never the right answer for a deployable.</summary>
    IEnumerable<string> PublishProjects => [];

    sealed AbsolutePath PublishDirectory => NukeBuild.RootDirectory / "artifacts" / "publish";

    /// <summary>
    /// The projects paired with the directory each publishes into. Defaults to <see cref="PublishProjects" />
    /// under <see cref="PublishDirectory" />, one folder per project name. Override when a later step - a
    /// packaging archive, a container build, a deploy script - reads the output from a path it fixes itself,
    /// rather than one derived from the project's file name. Note that only <see cref="PublishDirectory" />
    /// is declared as this target's artifacts, so an override pointing elsewhere publishes there but does not
    /// hand those files to CI as artifacts.
    /// </summary>
    IEnumerable<AppPublishEntry> PublishEntries =>
        PublishProjects.Select(project =>
            new AppPublishEntry(project, PublishDirectory / AppPublishLayout.OutputName(project)));

    /// <summary>
    /// Runtime identifier to publish for (e.g. <c>linux-arm64</c>). Null publishes portable, without a RID.
    /// <para>
    /// Setting this makes the build runtime-specific end to end: <c>--no-build</c> only finds artifacts
    /// under <c>obj/{Configuration}/{TargetFramework}/{Runtime}/</c>, so Restore and Compile must have run
    /// for the same RID and the same projects.
    /// </para>
    /// </summary>
    string? PublishRuntime => null;

    /// <summary>
    /// Whether a RID-specific publish carries its own runtime. Ignored without <see cref="PublishRuntime" />.
    /// Stated explicitly rather than left to the SDK, whose default for <c>-r</c> has changed between
    /// versions - the difference is a self-contained output several times the size of a portable one.
    /// </summary>
    bool PublishSelfContained => false;

    /// <summary>
    /// Target framework moniker to publish. Null publishes the project's own framework, which is what a
    /// single-target project wants; set it for a multi-targeting project, where <c>dotnet publish</c> cannot
    /// choose one for you.
    /// </summary>
    string? PublishFramework => null;

    Target PublishApp =>
        _ => _.Description("Publishes the deployable projects (exercises the publish pipeline, not just build)")
            .DependsOn(Compile)
            .Produces(PublishDirectory / "**")
            .Executes(() =>
            {
                var entries = PublishEntries.ToList();

                if (entries.Count == 0)
                {
                    Log.Warning("PublishApp: no PublishProjects declared - nothing to publish.");

                    return;
                }

                AppPublishLayout.EnsureNoOutputCollision(entries);

                foreach (var entry in entries)
                {
                    // Cleaned per entry, not as one tree: an entry may point outside PublishDirectory, and
                    // wiping a directory this target does not own would take a sibling build's output with it.
                    entry.OutputDirectory.CreateOrCleanDirectory();

                    Log.Information("Publishing {Project} ({Runtime}) -> {Output}",
                        entry.ProjectPath,
                        PublishRuntime ?? "portable",
                        entry.OutputDirectory);

                    // NoBuild: Compile already produced this configuration. Publish still runs its own
                    // pipeline on top of those outputs, which is exactly what this target is for.
                    DotNetPublish(s =>
                    {
                        s = s.SetProject(entry.ProjectPath)
                            .SetConfiguration(Configuration)
                            .EnableNoBuild()
                            .SetOutput(entry.OutputDirectory);

                        if (PublishRuntime is not null)
                            s = s.SetRuntime(PublishRuntime).SetSelfContained(PublishSelfContained);

                        if (PublishFramework is not null)
                            s = s.SetFramework(PublishFramework);

                        return s;
                    });
                }
            });
}
