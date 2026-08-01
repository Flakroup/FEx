using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace FEx.Building;

/// <summary>
/// Aggregates Microsoft.Testing.Platform cobertura reports into a per-file coverage verdict.
/// </summary>
/// <remarks>
/// A solution-wide MTP run emits one cobertura file per test assembly, and the same production class
/// is frequently exercised by several of them. Coverage therefore has to be merged as a UNION of
/// covered line numbers per source file - averaging the per-report rates would under-report a class
/// that two test projects cover between them.
/// </remarks>
public static class CoverageGate
{
    /// <summary>Analyses already-loaded cobertura documents. Pure - the unit-test entry point.</summary>
    public static CoverageReport Analyze(IEnumerable<XDocument> reports, CoverageGateOptions options)
    {
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentNullException.ThrowIfNull(options);

        var byFile = new Dictionary<string, LineSets>(StringComparer.OrdinalIgnoreCase);
        var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var report in reports)
        {
            foreach (var package in report.Descendants("package"))
            {
                var module = package.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(module))
                    modules.Add(module);
            }

            foreach (var @class in report.Descendants("class"))
            {
                var file = @class.Attribute("filename")?.Value;
                if (string.IsNullOrEmpty(file))
                    continue;

                var relative = Relativize(file, options.RootDirectory);
                if (relative is null || !IsInScope(relative, options))
                    continue;

                if (!byFile.TryGetValue(relative, out var lines))
                    byFile[relative] = lines = new LineSets();

                Collect(@class, lines);
            }
        }

        var stale = ApplyMarkers(byFile, options);

        var files = byFile
            .Select(entry => entry.Value.ToFile(entry.Key))
            .OrderByDescending(static f => f.UncoveredLines.Count)
            .ThenBy(static f => f.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var missing = options.ExpectedModules
            .Where(expected => !modules.Contains(expected))
            .OrderBy(static m => m, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new CoverageReport(
            files,
            modules.OrderBy(static m => m, StringComparer.OrdinalIgnoreCase).ToList(),
            missing,
            stale);
    }

    /// <summary>
    /// Reads each policed source file and exempts the lines carrying the marker, reporting every marker
    /// that exempts nothing.
    /// <para>
    /// The marker lives ON the line it excuses, which is the whole point: a line number kept in the build
    /// script has no anchor in the file, so any edit above it slides the exemption onto a neighbour -
    /// silently, and usually onto a line that IS covered, which turns the gate green for the wrong reason.
    /// A comment moves with the code it belongs to, and disappears with it.
    /// </para>
    /// </summary>
    private static IReadOnlyList<StaleExclusion> ApplyMarkers(
        Dictionary<string, LineSets> byFile, CoverageGateOptions options)
    {
        if (options.ReadSourceLines is null || string.IsNullOrWhiteSpace(options.LineExclusionMarker))
            return [];

        List<StaleExclusion> stale = [];
        foreach ((var path, var lines) in byFile)
        {
            var source = options.ReadSourceLines(path);
            if (source is null)
                continue;

            for (var index = 0; index < source.Count; index++)
            {
                if (MarkerOn(source[index], options.LineExclusionMarker) is not (var span, var reason))
                    continue;

                var first = index + 1;
                if (reason.Length == 0)
                    stale.Add(new StaleExclusion(path, first, StaleReason.NoReasonGiven));

                // Every line of the span is judged on its own: a span that reaches past what it was written
                // for is the same silent over-reach as a drifting line number, only spelled out in the source.
                for (var number = first; number < first + span; number++)
                {
                    if (reason.Length > 0)
                    {
                        if (lines.Covered.Contains(number))
                            stale.Add(new StaleExclusion(path, number, StaleReason.LineIsCovered));
                        else if (!lines.Measurable.Contains(number))
                            stale.Add(new StaleExclusion(path, number, StaleReason.NothingToExclude));
                    }

                    lines.Measurable.Remove(number);
                    lines.Covered.Remove(number);
                }
            }
        }

        return stale
            .OrderBy(static s => s.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static s => s.Line)
            .ToList();
    }

    /// <summary>
    /// How many lines the marker on this line covers and why, or null when it carries none. Bare
    /// <c>coverage-exclude:</c> covers this line alone; <c>coverage-exclude+2:</c> covers this line and the
    /// two below it - the escape hatch for a statement split across lines, where the continuation lines are
    /// inside a string literal and cannot hold a comment of their own.
    /// </summary>
    private static (int Span, string Reason)? MarkerOn(string sourceLine, string marker)
    {
        var core = marker.TrimEnd(':');
        var at = sourceLine.IndexOf(core, StringComparison.Ordinal);
        if (at < 0)
            return null;

        var rest = sourceLine[(at + core.Length)..];
        var span = 1;

        if (rest.StartsWith('+'))
        {
            var end = 1;
            while (end < rest.Length && char.IsAsciiDigit(rest[end]))
                end++;

            if (end > 1 && int.TryParse(rest[1..end], out var extra) && extra > 0)
                span = extra + 1;

            rest = rest[end..];
        }

        // A marker written without its colon reports as reasonless rather than being ignored: silently
        // doing nothing is the behaviour this whole mechanism exists to stamp out.
        return (span, rest.StartsWith(':') ? rest[1..].Trim() : string.Empty);
    }

    /// <summary>
    /// Loads cobertura files from disk, then analyses them. Source files are read from
    /// <see cref="CoverageGateOptions.RootDirectory" /> unless the caller supplied its own reader - which
    /// is what keeps <see cref="Analyze" /> pure and testable without a filesystem.
    /// </summary>
    public static CoverageReport AnalyzeFiles(IEnumerable<string> coberturaFiles, CoverageGateOptions options)
    {
        ArgumentNullException.ThrowIfNull(coberturaFiles);
        ArgumentNullException.ThrowIfNull(options);

        var withReader = options.ReadSourceLines is not null
            ? options
            : options with { ReadSourceLines = path => ReadFromDisk(options.RootDirectory, path) };

        return Analyze(coberturaFiles.Select(XDocument.Load).ToList(), withReader);
    }

    /// <summary>Missing or unreadable source is not a gate failure: the report can name a file this
    /// checkout does not have (a submodule built elsewhere), and that says nothing about coverage.</summary>
    private static IReadOnlyList<string>? ReadFromDisk(string rootDirectory, string relativePath)
    {
        try
        {
            var full = Path.Combine(rootDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));

            return File.Exists(full) ? File.ReadAllLines(full) : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static void Collect(XElement @class, LineSets lines)
    {
        // <lines> appears both under <class> and under each <method>; Descendants covers both, and the
        // sets dedupe the overlap.
        foreach (var line in @class.Descendants("line"))
        {
            if (!int.TryParse(line.Attribute("number")?.Value, out var number))
                continue;

            lines.Measurable.Add(number);

            if (int.TryParse(line.Attribute("hits")?.Value, out var hits) && hits > 0)
                lines.Covered.Add(number);
        }
    }

    /// <summary>
    /// Turns an absolute path from the report into a root-relative one with forward slashes, or null
    /// when the file lives outside the repository.
    /// </summary>
    private static string? Relativize(string absolutePath, string rootDirectory)
    {
        var normalized = absolutePath.Replace('\\', '/');

        if (string.IsNullOrEmpty(rootDirectory))
            return normalized;

        var root = rootDirectory.Replace('\\', '/').TrimEnd('/') + "/";

        return normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? normalized[root.Length..]
            : null;
    }

    private static bool IsInScope(string relativePath, CoverageGateOptions options)
    {
        // Source generators emit into obj/; that is code nobody wrote, so it never faces the gate.
        if (relativePath.Split('/').Any(static segment => segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            return false;

        if (options.IncludedPrefixes.Count > 0 && !options.IncludedPrefixes.Any(prefix => Matches(relativePath, prefix)))
            return false;

        return !options.Exclusions.Any(exclusion => Matches(relativePath, exclusion));
    }

    /// <summary>Matches a whole file or a directory subtree - no globbing, so a pattern cannot silently over-match.</summary>
    private static bool Matches(string relativePath, string pattern)
    {
        var normalized = pattern.Replace('\\', '/').TrimEnd('/');

        return relativePath.Equals(normalized, StringComparison.OrdinalIgnoreCase)
               || relativePath.StartsWith(normalized + "/", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class LineSets
    {
        public HashSet<int> Measurable { get; } = [];
        public HashSet<int> Covered { get; } = [];

        public CoverageFile ToFile(string path) =>
            new(path,
                Measurable.Count,
                Covered.Count,
                Measurable.Except(Covered).OrderBy(static n => n).ToList());
    }
}

/// <summary>Inputs that decide what the gate looks at and what it forgives.</summary>
public sealed record CoverageGateOptions
{
    /// <summary>Repository root; report paths are made relative to it.</summary>
    public string RootDirectory { get; init; } = string.Empty;

    /// <summary>Root-relative subtrees the gate covers, e.g. "src". Empty means everything.</summary>
    public IReadOnlyList<string> IncludedPrefixes { get; init; } = [];

    /// <summary>Root-relative files or subtrees exempt from the threshold. Every entry needs a written reason.</summary>
    public IReadOnlyList<string> Exclusions { get; init; } = [];

    /// <summary>
    /// The comment marker that exempts the source line it appears on, followed by the reason:
    /// <c>} // coverage-exclude: closing brace of a for(;;) whose every path returns</c>. For a single
    /// defensive branch inside an otherwise fully-tested file, where excluding the whole file would throw
    /// away real coverage.
    /// <para>
    /// It lives in the source rather than in the build script because a line NUMBER has no anchor in the
    /// file: every line inserted above it slides the exemption onto a neighbour, usually onto a covered
    /// line, and the gate then passes for the wrong reason with nothing to notice. A comment travels with
    /// its code and dies with it. The reason is mandatory - a bare marker is reported as stale.
    /// </para>
    /// </summary>
    public string LineExclusionMarker { get; init; } = "coverage-exclude:";

    /// <summary>
    /// Reads a policed source file (root-relative path, forward slashes) into its lines, or returns null
    /// when it cannot be read. Left null, no source is scanned and no marker applies - which is how
    /// <see cref="CoverageGate.Analyze" /> stays pure; <see cref="CoverageGate.AnalyzeFiles" /> fills it in
    /// with a filesystem reader.
    /// </summary>
    public Func<string, IReadOnlyList<string>?>? ReadSourceLines { get; init; }

    /// <summary>
    /// Assemblies that must show up in the reports. A project whose assembly is absent contributes zero
    /// measurable lines, which would otherwise pass the gate vacuously.
    /// </summary>
    public IReadOnlyList<string> ExpectedModules { get; init; } = [];
}

/// <summary>Coverage of one source file, merged across every report that mentions it.</summary>
public sealed record CoverageFile(string Path, int MeasurableLines, int CoveredLines, IReadOnlyList<int> UncoveredLines)
{
    public bool IsFullyCovered => UncoveredLines.Count == 0;

    public double Rate => MeasurableLines == 0 ? 1d : (double)CoveredLines / MeasurableLines;
}

/// <summary>Why a marker in the source exempts nothing.</summary>
public enum StaleReason
{
    /// <summary>The line it sits on is covered by tests - so the marker excuses a line that needs no
    /// excuse, and would go on hiding the line if a real gap ever appeared there.</summary>
    LineIsCovered,

    /// <summary>The line carries no instrumented code at all: the statement the marker was written for
    /// has moved or gone, and the comment stayed behind.</summary>
    NothingToExclude,

    /// <summary>The marker carries no reason. An exemption nobody can review is not an exemption.</summary>
    NoReasonGiven,
}

/// <summary>A marker that no longer earns its place, and why.</summary>
public sealed record StaleExclusion(string Path, int Line, StaleReason Reason);

/// <summary>The aggregated verdict.</summary>
public sealed record CoverageReport(
    IReadOnlyList<CoverageFile> Files,
    IReadOnlyList<string> Modules,
    IReadOnlyList<string> MissingModules,
    IReadOnlyList<StaleExclusion> StaleExclusions)
{
    public int MeasurableLines => Files.Sum(static f => f.MeasurableLines);

    public int CoveredLines => Files.Sum(static f => f.CoveredLines);

    public double Rate => MeasurableLines == 0 ? 1d : (double)CoveredLines / MeasurableLines;

    public IReadOnlyList<CoverageFile> IncompleteFiles => Files.Where(static f => !f.IsFullyCovered).ToList();

    /// <summary>
    /// True when anything is under the threshold, when an expected assembly never reported, or when a
    /// marker exempts nothing. The last one is a failure rather than a warning on purpose: a dead
    /// exemption is how the gate quietly stops guarding the thing it was written for.
    /// </summary>
    public bool Failed => IncompleteFiles.Count > 0 || MissingModules.Count > 0 || StaleExclusions.Count > 0;
}
