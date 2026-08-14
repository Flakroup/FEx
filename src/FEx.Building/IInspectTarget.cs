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

    Target Inspect =>
        _ => _.Description("Fails on any ReSharper finding at or above the declared severity")
            .DependsOn(Compile)
            .Executes(() =>
            {
                var manifest = RootDirectory / ".config" / "dotnet-tools.json";
                if (!File.Exists(manifest))
                    throw new InvalidOperationException(
                        $"No local tool manifest at {manifest}. The inspection runs the version this "
                        + "repository pins, so create it with `dotnet new tool-manifest` and add the tool "
                        + "with `dotnet tool install JetBrains.ReSharper.GlobalTools`.");

                InspectionReport.Parent.CreateDirectory();

                RunDotNet("tool restore");
                RunDotNet(InspectionArguments(
                    Solution.Path!,
                    InspectionReport,
                    FreshCaches(InspectionCachesDirectory),
                    Configuration,
                    InspectionSeverity));

                Verdict(InspectionGate.Analyze(File.ReadAllText(InspectionReport)));
            });

    /// <summary>
    /// The whole command line, as one string. Static and pure so the flags that decide the verdict are
    /// pinned by a unit test rather than by whoever reads the target next.
    /// </summary>
    /// <remarks>
    /// <c>--no-build</c> is affordable only because <see cref="Inspect" /> depends on the compile step, and
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
    /// Starts <c>dotnet</c> with the command line composed here, verbatim.
    /// </summary>
    /// <remarks>
    /// Not <c>DotNetTasks.DotNet</c>, and the difference is not stylistic. That overload takes an
    /// <c>ArgumentStringHandler</c>, which wraps a pre-composed string carrying quotes into a single
    /// argument - measured: <c>dotnet</c> answered "the command or file was not found", having been handed
    /// the whole inspection as one token. Quoting paths is not optional here, because a checkout under a
    /// path with a space is ordinary, so the process is started with the arguments as written.
    /// </remarks>
    private static void RunDotNet(string arguments)
    {
        using var process = ProcessTasks.StartProcess("dotnet", arguments, NukeBuild.RootDirectory);

        process.AssertZeroExitCode();
    }

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
