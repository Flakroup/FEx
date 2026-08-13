using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using System;
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
        PublishProjects.Select(project =>
        {
            var path = ResolvePublishProject(project);

            // The output name comes from the project FILE, never from the name given here: dropping "the
            // extension" off a bare "Sample.Api" leaves "Sample", and dotted project names are the norm.
            return new AppPublishEntry(path, PublishDirectory / AppPublishLayout.OutputName(path));
        });

    /// <summary>
    /// Finds a declared publish project in the solution by name. Reports what was asked for and what the
    /// solution actually holds - the bare <c>Single</c> answers a typo with "Sequence contains no matching
    /// element", which names neither.
    /// </summary>
    /// <exception cref="InvalidOperationException">No project, or more than one, carries that name.</exception>
    sealed AbsolutePath ResolvePublishProject(string project)
    {
        var matches = Solution.Projects.Where(p => p.Name == project).ToList();

        return matches.Count == 1
            ? matches[0].Path
            : throw new InvalidOperationException(
                $"PublishProjects names '{project}', which matches {matches.Count} projects in "
                + $"{Solution.Name}. Available: {string.Join(", ", Solution.Projects.Select(p => p.Name).OrderBy(static n => n))}.");
    }

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

    /// <summary>
    /// Restores the solution, and - when publishing for a runtime - the publish projects again for that RID.
    /// </summary>
    /// <remarks>
    /// The RID pass is an ADDITION, never a replacement. Narrowing the run to the publish projects left every
    /// project they do not reference unrestored and unbuilt - which is every test project, since tests
    /// reference the libraries and nothing references tests. <c>dotnet test --no-build</c> does not fail on
    /// missing input, it runs whatever assemblies are on disk, so a single invocation combining
    /// <c>PublishApp</c> with <c>Test</c> reported a GREEN build over a failing test (measured).
    /// </remarks>
    virtual IReadOnlyCollection<Output> OnRestore() =>
    [
        .. Scope().SelectMany(step => DotNetRestore(s =>
            GetRestoreSettings(s, step.Project, Configuration)
                .WithRuntime(step.WithRuntime ? PublishRuntime : null)))
    ];

    sealed IEnumerable<(AbsolutePath Project, bool WithRuntime)> Scope() =>
        Scope(Solution, RuntimeSpecificPublishPass, PublishEntries);

    /// <summary>
    /// Everything Restore and Compile pass over: the solution, then - only when this run publishes for a
    /// runtime - each publish project again, that time carrying the RID.
    /// </summary>
    /// <remarks>
    /// The solution entry is unconditional, and that is the whole point. While the RID pass REPLACED it,
    /// every project the publish entries do not reference went unrestored and unbuilt - which is every test
    /// project, since tests reference the libraries and nothing references tests. <c>dotnet test --no-build</c>
    /// does not fail on missing input, so a run combining <c>PublishApp</c> with <c>Test</c> reported a green
    /// build over a failing test. Static and pure so this decision is testable without running a build.
    /// </remarks>
    public static IEnumerable<(AbsolutePath Project, bool WithRuntime)> Scope(
        AbsolutePath solution,
        bool runtimeSpecific,
        IEnumerable<AppPublishEntry> entries) =>
    [
        (solution, false),
        .. runtimeSpecific
            ? entries.Select(static entry => (entry.ProjectPath, true))
            : []
    ];

    virtual DotNetRestoreSettings GetRestoreSettings(DotNetRestoreSettings settings,
                                                     AbsolutePath solution,
                                                     Configuration? configuration = null) =>
        settings.SetProjectFile(solution)
            .When(_ => configuration is not null, s => s.SetProperty("Configuration", configuration!.ToString()));

    /// <summary>
    /// Builds the solution, and - when publishing for a runtime - the publish projects again for that RID,
    /// which is what lets the later <c>--no-build</c> publish find artifacts under
    /// <c>obj/{Configuration}/{TargetFramework}/{Runtime}/</c>. See <see cref="OnRestore" /> for why this
    /// adds to the solution build rather than replacing it.
    /// </summary>
    virtual IReadOnlyCollection<Output> OnCompile() =>
    [
        .. Scope().SelectMany(step => DotNetBuild(s =>
            GetBuildSettings(s, step.Project).WithRuntime(step.WithRuntime ? PublishRuntime : null)))
    ];

    /// <summary>Whether this run needs the extra RID-specific pass over the publish projects.</summary>
    protected bool RuntimeSpecificPublishPass => PublishRuntime is not null && IsPublishScheduled;

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