using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
using System.Linq;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

/// <summary>
/// Publishes deployable applications. Distinct from <see cref="INuGetPublishTarget" />, which pushes
/// packages to a feed - hence <c>PublishApp</c> rather than <c>Publish</c>, so a repo can implement both.
/// <para>
/// This exists because <c>dotnet build</c> and <c>dotnet test</c> never exercise publish pipeline, and
/// for anything with a Blazor WebAssembly client that pipeline is where the real work happens: assembly
/// linking, the <c>blazor.boot.json</c> manifest, compression, static asset fingerprinting. A repo whose CI
/// only builds and tests can be green while its deployable output is broken - the failure then surfaces in
/// the container build, after the merge.
/// </para>
/// </summary>
public interface IAppPublishTarget : ICompileTarget
{
    /// <summary>
    /// Whether a RID-specific publish carries its own runtime. Ignored without <see cref="ICompileTarget.PublishRuntime" />.
    /// Stated explicitly rather than left to the SDK, whose default for <c>-r</c> has changed between
    /// versions - the difference is a self-contained output several times the size of a portable one.
    /// </summary>
    bool PublishSelfContained { get; }

    /// <summary>
    /// Indicates whether to publish as a single-file application.
    /// </summary>
    bool PublishSingleFile { get; }

    /// <summary>
    /// Target framework moniker to publish. Null publishes the project's own framework, which is what a
    /// single-target project wants; set it for a multi-targeting project, where <c>dotnet publish</c> cannot
    /// choose one for you.
    /// </summary>
    string? PublishFramework { get; }

    Target PublishApp =>
        _ => _.Description("Publishes the deployable projects")
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

                    DotNetPublish(s => GetPublishSettings(s, entry));
                }
            });

    virtual DotNetPublishSettings GetPublishSettings(DotNetPublishSettings s, AppPublishEntry entry) =>
        s.SetProject(entry.ProjectPath)
            .SetConfiguration(Configuration)
            .EnableNoBuild()
            .SetOutput(entry.OutputDirectory)
            .When(_ => PublishRuntime is not null,
                c => c.SetRuntime(PublishRuntime).SetSelfContained(PublishSelfContained))
            .When(_ => PublishFramework is not null, c => c.SetFramework(PublishFramework))
            .When(_ => PublishSingleFile, static c => c.EnablePublishSingleFile());
}