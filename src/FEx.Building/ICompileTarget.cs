using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using System.Collections.Generic;
using System.Linq;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

public interface ICompileTarget : INukeBuild
{
    Solution Solution { get; }
    Configuration Configuration { get; }

    /// <summary>
    /// Projects to publish, one <c>dotnet publish</c> each. Empty by default: publishing is
    /// opt-in, and "the whole solution" is never the right answer for a deployable.
    /// </summary>
    IEnumerable<string> PublishProjects { get; }

    sealed AbsolutePath PublishDirectory => NukeBuild.RootDirectory / "artifacts" / "publish";

    /// <summary>
    /// The projects paired with the directory each publishes into. Defaults to <see cref="PublishProjects" />
    /// under <see cref="PublishDirectory" />, one folder per project name. Override when a later step - a
    /// packaging archive, a container build, deploy script - reads the output from a path it fixes itself,
    /// rather than one derived from the project's file name. Note that only <see cref="PublishDirectory" />
    /// is declared as this target's artifacts, so an override pointing elsewhere publishes there but does not
    /// hand those files to CI as artifacts.
    /// </summary>
    IEnumerable<AppPublishEntry> PublishEntries =>
        PublishProjects.Select(project => new AppPublishEntry(Solution.Projects.Single(p => p.Name == project),
            PublishDirectory / AppPublishLayout.OutputName(project)));

    /// <summary>
    /// Runtime identifier to publish for (e.g. <c>linux-arm64</c>). Null publishes portable, without a RID.
    /// <para>
    /// Setting this makes the build runtime-specific end to end: <c>--no-build</c> only finds artifacts
    /// under <c>obj/{Configuration}/{TargetFramework}/{Runtime}/</c>, so Restore and Compile must have run
    /// for the same RID and the same projects.
    /// </para>
    /// </summary>
    string? PublishRuntime { get; }

    Target Restore => _ => _.Executes(OnRestore);

    Target Compile => _ => _.DependsOn(Restore).Executes(OnCompile);

    /// <summary>
    /// True when the <see cref="IAppPublishTarget.PublishApp" /> target is in the scheduled
    /// execution plan. Used by <see cref="Restore" />/<see cref="Compile" /> to decide
    /// between solution-wide and per-project per-runtime invocations.
    /// </summary>
    protected bool IsPublishScheduled => ExecutionPlan.Any(static t => t.Name == nameof(IAppPublishTarget.PublishApp));

    virtual IReadOnlyCollection<Output> OnRestore() =>
        PublishRuntime is null || !IsPublishScheduled
            ? DotNetRestore(s => GetRestoreSettings(s, Solution, Configuration))
            :
            [
                .. PublishEntries.SelectMany(entry => DotNetRestore(s =>
                    GetRestoreSettings(s, entry.ProjectPath, Configuration).WithRuntime(PublishRuntime)))
            ];

    virtual DotNetRestoreSettings GetRestoreSettings(DotNetRestoreSettings settings,
                                                     AbsolutePath solution,
                                                     Configuration? configuration = null) =>
        settings.SetProjectFile(solution)
            .When(_ => configuration is not null, s => s.SetProperty("Configuration", configuration!.ToString()));

    virtual IReadOnlyCollection<Output> OnCompile() =>
        PublishRuntime is null || !IsPublishScheduled
            ? DotNetBuild(s => GetBuildSettings(s, Solution))
            :
            [
                .. PublishEntries.SelectMany(entry => DotNetBuild(s =>
                    GetBuildSettings(s, entry.ProjectPath).WithRuntime(PublishRuntime)))
            ];

    virtual DotNetBuildSettings GetBuildSettings(DotNetBuildSettings settings,
                                                 AbsolutePath solution,
                                                 bool noRestore = true,
                                                 DotNetVerbosity? verbosity = null) =>
        settings.SetConfiguration(Configuration)
            .SetNoRestore(noRestore)
            .SetProjectFile(solution)
            .SetProcessAdditionalArguments("-m", "-bl")
            .When(_ => verbosity is not null, s => s.SetVerbosity(verbosity));
}