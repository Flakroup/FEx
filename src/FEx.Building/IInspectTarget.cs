using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FEx.Building;

/// <summary>
/// Fails the build on any ReSharper finding at or above the declared severity, over the solution the
/// compile step just built.
/// </summary>
/// <remarks>
/// A build target rather than a command a person is asked to remember. The inspection used to be exactly
/// that - a line in a conventions file - and errors got past it repeatedly, each time discovered by
/// whoever opened the NEXT branch and inherited a debt they had not incurred.
/// <para>
/// Three properties of the command line are load-bearing and each was measured rather than assumed:
/// the report path is mandatory (without it the tool dies inside its own logger instead of running), the
/// caches directory must be EMPTY (a directory reused across changed types invents
/// <c>Cannot resolve symbol</c>), and solution-wide analysis has to be forced ON - with it off the same
/// solution produced four findings the full run does not report, so the two modes are different gates
/// rather than a cheap and an expensive version of one.
/// </para>
/// <para>
/// It runs ALONGSIDE the suite rather than after it: <see cref="TriggerInspect" /> starts the tool right
/// after the compile step and <see cref="Inspect" /> only collects the verdict, so a build asking for both
/// ends when the longer of the two does. The inspection is the longest step of such a build, and neither
/// step reads what the other writes. A build whose suite goes red never reaches <see cref="Inspect" />;
/// the run still going is killed when the build ends and its verdict is dropped - a redundant cast is
/// worth nothing while a test is red.
/// </para>
/// <para>
/// The tool is a LOCAL dotnet tool, pinned in the consuming repository's <c>.config/dotnet-tools.json</c>.
/// That is what keeps one version in play: a gate that installs its own copy disagrees with the command a
/// developer runs the moment either is bumped, and a gate that argues with the developer is not a gate.
/// </para>
/// <para>
/// Known and deliberately unhandled: the run can write <c>SettingsMigration</c> markers and a byte order
/// mark back into an injected settings layer. On the pins carried today it writes nothing, because those
/// markers are already committed - so the churn is a thing to discard when it appears, not a thing this
/// target reverts. Reverting would mean a build component running <c>git checkout</c> over a working tree
/// it does not own.
/// </para>
/// </remarks>
public interface IInspectTarget : ICompileTarget
{
    /// <summary>
    /// The minimum severity that reaches the report - and therefore the gate's whole definition of a
    /// finding, since anything the report holds fails the build.
    /// </summary>
    string InspectionSeverity => "ERROR";

    sealed AbsolutePath InspectionReport =>
        NukeBuild.RootDirectory / "artifacts" / "inspection" / "inspectcode.sarif";

    /// <summary>
    /// Cleaned before every run, and that is not tidiness. A caches directory carried across a changed
    /// type has been measured producing <c>Cannot resolve symbol</c> errors for symbols that resolve
    /// perfectly - a gate reddening at random on findings that do not exist teaches people to ignore it.
    /// </summary>
    sealed AbsolutePath InspectionCachesDirectory => NukeBuild.TemporaryDirectory / "inspectcode-caches";

    /// <summary>
    /// Starts the inspection and moves on, so it runs alongside the suite; <see cref="Inspect" /> collects
    /// its verdict later. Only ever scheduled through <see cref="Inspect" />, which depends on it.
    /// </summary>
    /// <remarks>
    /// Ordered before the test target when the build has one - <c>Before</c> is an ordering, not a
    /// dependency, so a build asking for the inspection alone still gets it without the suite. Nothing
    /// here waits: the tool reads the tree Compile left and writes only its own report and caches, which
    /// the suite never touches. A build that stops before <see cref="Inspect" /> - a red test - has
    /// <see cref="InspectionRun.DiscardPending" /> kill what is still running.
    /// </remarks>
    Target TriggerInspect =>
        _ =>
        {
            var definition = _.Description("Starts the ReSharper inspection in the background")
                .DependsOn(Compile)
                .Executes(() => NewRun().StartInBackground());

            return this is ITestTarget tests ? definition.Before(tests.Test) : definition;
        };

    Target Inspect =>
        _ => _.Description("Fails on any ReSharper finding at or above the declared severity")
            // Compile is named here as well as through TriggerInspect: skipping a target on the command
            // line skips every dependency nothing else scheduled asks for, so without its own edge
            // `--skip TriggerInspect` would take Compile with it and inspect whatever tree is on disk.
            .DependsOn(TriggerInspect, Compile)
            .Executes(() =>
            {
                // Nothing pending when TriggerInspect was skipped on the command line: the inspection
                // then runs here, in full, as it did before it could be started early.
                Verdict((InspectionRun.Pending ?? NewRun()).Collect());
            });

    /// <summary>
    /// The run, composed here and started by whichever target gets to it first. Checks for the tool
    /// manifest on the way, so the guided message comes before a background thread's "command not found".
    /// </summary>
    /// <remarks>
    /// The process is started with the arguments as written - not through <c>DotNetTasks.DotNet</c>, and
    /// the difference is not stylistic. That overload takes an <c>ArgumentStringHandler</c>, which wraps a
    /// pre-composed string carrying quotes into a single argument - measured: <c>dotnet</c> answered "the
    /// command or file was not found", having been handed the whole inspection as one token. Quoting paths
    /// is not optional here, because a checkout under a path with a space is ordinary.
    /// </remarks>
    private InspectionRun NewRun()
    {
        var manifest = RootDirectory / ".config" / "dotnet-tools.json";
        if (!File.Exists(manifest))
            throw new InvalidOperationException(
                $"No local tool manifest at {manifest}. The inspection runs the version this "
                + "repository pins, so create it with `dotnet new tool-manifest` and add the tool "
                + "with `dotnet tool install JetBrains.ReSharper.GlobalTools`.");

        InspectionReport.Parent.CreateDirectory();

        return new InspectionRun(InspectionArguments(Solution.Path!,
                InspectionReport,
                FreshCaches(InspectionCachesDirectory),
                Configuration,
                InspectionSeverity),
            () => File.ReadAllText(InspectionReport),
            // Neither the invocation nor the output is logged as it happens: InspectionRun buffers both
            // for the target that collects the verdict.
            static arguments => new ProcessTree(ProcessTasks.StartProcess("dotnet",
                arguments,
                NukeBuild.RootDirectory,
                logOutput: false,
                logInvocation: false)));
    }

    /// <summary>
    /// The whole command line, as one string. Static and pure so the flags that decide the verdict are
    /// pinned by a unit test rather than by whoever reads the target next.
    /// </summary>
    /// <remarks>
    /// <c>--no-build</c> is affordable only because the run starts after the compile step, and
    /// it carries the configuration for the same reason: the tool evaluates MSBuild itself, so left to its
    /// own default it would look for a Debug build that a release run never produced and report the whole
    /// solution as unresolvable.
    /// </remarks>
    public static string InspectionArguments(AbsolutePath solution,
                                             AbsolutePath report,
                                             AbsolutePath cachesDirectory,
                                             Configuration configuration,
                                             string severity) =>
        string.Join(' ',
            "jb", "inspectcode", Quote(solution),
            $"-e={severity}",
            "--swea",
            "--no-build",
            $"--properties:Configuration={configuration}",
            $"-o={Quote(report)}",
            $"--caches-home={Quote(cachesDirectory)}");

    /// <summary>
    /// Empties the caches directory and hands it back, so the command line can only ever be composed
    /// around a fresh one.
    /// </summary>
    /// <remarks>
    /// On the path to the argument rather than a statement beside it, and public rather than buried in the
    /// target body, because this is the determinism the whole gate rests on: a caches directory carried
    /// across a changed type has been measured inventing <c>Cannot resolve symbol</c> for symbols that
    /// resolve. Deleting the clean was measured leaving the entire suite green, which is exactly the shape
    /// of regression a gate cannot afford to ship.
    /// </remarks>
    public static AbsolutePath FreshCaches(AbsolutePath cachesDirectory)
    {
        cachesDirectory.CreateOrCleanDirectory();

        return cachesDirectory;
    }

    private static string Quote(AbsolutePath path) => $"\"{path}\"";

    /// <summary>
    /// Names every finding and then fails the build if there was one. Public and static because "a
    /// finding does not stop the build" is the one way this gate can be broken while every other test in
    /// the suite stays green, so it is pinned directly rather than through the target's body.
    /// </summary>
    /// <exception cref="InvalidOperationException">The report holds at least one finding.</exception>
    public static void Verdict(IReadOnlyList<InspectionFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);

        foreach (var finding in findings.OrderBy(static f => f.File, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static f => f.Line))
            Log.Error("  {Finding}", finding.ToString());

        if (findings.Count > 0)
            throw new InvalidOperationException(
                $"ReSharper inspection failed: {findings.Count} finding(s) at or above the declared "
                + "severity. Every one of them is a build error here, whatever it is called upstream.");

        Log.Information("ReSharper inspection: nothing at or above the declared severity");
    }
}
