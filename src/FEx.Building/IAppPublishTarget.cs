using System.Collections.Generic;
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

    Target PublishApp =>
        _ => _.Description("Publishes the deployable projects (exercises the publish pipeline, not just build)")
            .DependsOn(Compile)
            .Produces(PublishDirectory / "**")
            .Executes(() =>
            {
                var projects = new List<string>(PublishProjects);
                if (projects.Count == 0)
                {
                    Log.Warning("PublishApp: no PublishProjects declared - nothing to publish.");
                    return;
                }

                AppPublishLayout.EnsureNoOutputCollision(projects);
                PublishDirectory.CreateOrCleanDirectory();

                foreach (var project in projects)
                {
                    var output = PublishDirectory / AppPublishLayout.OutputName(project);

                    Log.Information("Publishing {Project} -> {Output}", project, output);

                    // NoBuild: Compile already produced this configuration. Publish still runs its own
                    // pipeline on top of those outputs, which is exactly what this target is for.
                    DotNetPublish(s => s.SetProject(project)
                        .SetConfiguration(Configuration)
                        .EnableNoBuild()
                        .SetOutput(output)
                        .SetProperty("NuGetAudit", !NukeBuild.IsServerBuild));
                }
            });
}
