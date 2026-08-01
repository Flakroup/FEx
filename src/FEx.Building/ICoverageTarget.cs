using Nuke.Common;
using Nuke.Common.IO;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

/// <summary>
/// Runs the test suite with code coverage and fails the build when production code is not fully covered.
/// </summary>
/// <remarks>
/// The threshold is a hard 100% by design: any number between 0 and 100 turns into a negotiation, and a
/// gate that is negotiable stops being a gate. Genuinely untestable code leaves through
/// <see cref="CoverageExclusions" />, where every entry is visible in review and needs a written reason.
/// </remarks>
public interface ICoverageTarget : ICompileTarget
{
    sealed AbsolutePath CoverageDirectory => RootDirectory / "artifacts" / "coverage";

    /// <summary>Root-relative subtrees whose files must reach the threshold.</summary>
    /// <remarks>
    /// May reach outside the repository's own sources - e.g. a submodule built from source - so that
    /// borrowed code is held to the same bar as local code.
    /// </remarks>
    IReadOnlyList<string> CoverageIncludedPrefixes => ["src"];

    /// <summary>
    /// Root-relative subtrees holding the projects this repository OWNS: every one of them must appear
    /// in the reports.
    /// </summary>
    /// <remarks>
    /// Deliberately narrower than <see cref="CoverageIncludedPrefixes" />. A submodule ships far more
    /// projects than any one consumer references, and demanding coverage data from the unreferenced ones
    /// would fail the gate permanently; their files are still policed if they do get loaded.
    /// </remarks>
    IReadOnlyList<string> CoverageOwnedProjectPrefixes => ["src"];

    /// <summary>Root-relative files or subtrees exempt from the threshold. Each one needs a reason on record.</summary>
    IReadOnlyList<string> CoverageExclusions => [];

    /// <summary>
    /// The comment marker that exempts the source line it appears on, for a single defensive branch inside
    /// a file that is otherwise fully tested - where <see cref="CoverageExclusions" /> would throw away real
    /// coverage by exempting the whole file:
    /// <code>} // coverage-exclude: closing brace of a for(;;) whose every path returns from inside it</code>
    /// </summary>
    /// <remarks>
    /// Deliberately not a list of line numbers in this file. A number has no anchor in the source, so every
    /// line inserted above it slides the exemption onto a neighbour - and since the neighbour is usually
    /// covered, the gate goes on passing with nothing to notice. The marker travels with its code, dies with
    /// it, and carries its reason where the next reader is already looking. A marker that exempts nothing
    /// fails the gate rather than lingering.
    /// </remarks>
    string CoverageLineExclusionMarker => "coverage-exclude:";

    /// <summary>
    /// Projects under <see cref="CoverageOwnedProjectPrefixes" /> that legitimately produce no
    /// instrumentable code (interface-only or constant-only assemblies), so their absence is expected.
    /// </summary>
    IReadOnlyList<string> ModulesWithoutExecutableCode => [];

    Target Coverage =>
        _ => _.Description("Runs tests with coverage and fails below 100% outside the declared exclusions")
            .DependsOn(Compile)
            .Executes(() =>
            {
                CoverageDirectory.CreateOrCleanDirectory();

                // One solution-wide MTP run emits one cobertura file per test assembly.
                DotNet($"test --solution {Solution} --configuration {Configuration} --no-build " +
                       $"--coverage --coverage-output-format cobertura --results-directory {CoverageDirectory}");

                var reports = CoverageDirectory.GlobFiles("*.cobertura.xml");
                if (reports.Count == 0)
                    throw new InvalidOperationException(
                        $"No cobertura reports under {CoverageDirectory}. Coverage did not run, so the gate "
                        + "cannot vouch for anything.");

                var report = CoverageGate.AnalyzeFiles(
                    reports.Select(static path => path.ToString()),
                    new CoverageGateOptions
                    {
                        RootDirectory = RootDirectory,
                        IncludedPrefixes = CoverageIncludedPrefixes,
                        Exclusions = CoverageExclusions,
                        LineExclusionMarker = CoverageLineExclusionMarker,
                        ExpectedModules = ExpectedCoverageModules()
                    });

                Report(report, reports.Count);

                if (report.Failed)
                    throw new InvalidOperationException(
                        $"Coverage gate failed: {report.IncompleteFiles.Count} file(s) below 100%, "
                        + $"{report.MissingModules.Count} module(s) missing from the reports, "
                        + $"{report.StaleExclusions.Count} marker(s) exempting nothing.");
            });

    /// <summary>
    /// Every project on disk under the policed prefixes is expected to report, minus the ones declared
    /// as having no executable code. A new project with no tests at all therefore turns the gate red
    /// instead of passing unnoticed on zero measurable lines.
    /// </summary>
    /// <remarks>
    /// Read from the filesystem rather than the solution model on purpose: it also catches a project
    /// that exists but was never added to the solution, which would otherwise never be built or tested.
    /// </remarks>
    private IReadOnlyList<string> ExpectedCoverageModules() =>
        CoverageOwnedProjectPrefixes
            .Select(prefix => RootDirectory / prefix.Replace('\\', '/').TrimEnd('/'))
            .Where(static directory => Directory.Exists(directory))
            .SelectMany(static directory => Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories))
            .Select(static path => Path.GetFileNameWithoutExtension(path))
            .Where(name => !ModulesWithoutExecutableCode.Contains(name, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static void Report(CoverageReport report, int reportCount)
    {
        Log.Information("Coverage merged from {Reports} report(s): {Covered}/{Measurable} lines ({Rate:P1})",
            reportCount, report.CoveredLines, report.MeasurableLines, report.Rate);

        foreach (var module in report.Modules)
            Log.Information("  module {Module}", module);

        foreach (var missing in report.MissingModules)
            Log.Error("  MISSING module {Module} - it produced no coverage data at all", missing);

        foreach (var file in report.IncompleteFiles)
        {
            Log.Warning("  {Rate,6:P1} {File} - uncovered lines: {Lines}",
                file.Rate, file.Path, string.Join(", ", file.UncoveredLines));
        }

        // Named individually, with the line: the whole failure mode being fixed here is an exemption nobody
        // could tell had stopped applying, so a count alone would repeat the mistake.
        foreach (var stale in report.StaleExclusions)
            Log.Error("  STALE marker {File}:{Line} - {Reason}", stale.Path, stale.Line, Explain(stale.Reason));
    }

    private static string Explain(StaleReason reason) => reason switch
    {
        StaleReason.LineIsCovered => "the line is covered, so the marker excuses nothing - delete it",
        StaleReason.NothingToExclude => "no instrumented code on that line - the statement it was written for has moved",
        StaleReason.NoReasonGiven => "no reason after the marker - an exemption nobody can review is not one",
        _ => reason.ToString(),
    };
}
