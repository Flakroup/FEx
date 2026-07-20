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

        Dictionary<string, LineSets> byFile = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> modules = new(StringComparer.OrdinalIgnoreCase);

        foreach (XDocument report in reports)
        {
            foreach (XElement package in report.Descendants("package"))
            {
                string? module = package.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(module))
                    modules.Add(module);
            }

            foreach (XElement @class in report.Descendants("class"))
            {
                string? file = @class.Attribute("filename")?.Value;
                if (string.IsNullOrEmpty(file))
                    continue;

                string? relative = Relativize(file, options.RootDirectory);
                if (relative is null || !IsInScope(relative, options))
                    continue;

                if (!byFile.TryGetValue(relative, out LineSets? lines))
                    byFile[relative] = lines = new LineSets();

                Collect(@class, lines);
            }
        }

        List<CoverageFile> files = byFile
            .Select(entry => entry.Value.ToFile(entry.Key))
            .OrderByDescending(static f => f.UncoveredLines.Count)
            .ThenBy(static f => f.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<string> missing = options.ExpectedModules
            .Where(expected => !modules.Contains(expected))
            .OrderBy(static m => m, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new CoverageReport(files, modules.OrderBy(static m => m, StringComparer.OrdinalIgnoreCase).ToList(), missing);
    }

    /// <summary>Loads cobertura files from disk, then analyses them.</summary>
    public static CoverageReport AnalyzeFiles(IEnumerable<string> coberturaFiles, CoverageGateOptions options)
    {
        ArgumentNullException.ThrowIfNull(coberturaFiles);

        return Analyze(coberturaFiles.Select(XDocument.Load).ToList(), options);
    }

    private static void Collect(XElement @class, LineSets lines)
    {
        // <lines> appears both under <class> and under each <method>; Descendants covers both, and the
        // sets dedupe the overlap.
        foreach (XElement line in @class.Descendants("line"))
        {
            if (!int.TryParse(line.Attribute("number")?.Value, out int number))
                continue;

            lines.Measurable.Add(number);

            if (int.TryParse(line.Attribute("hits")?.Value, out int hits) && hits > 0)
                lines.Covered.Add(number);
        }
    }

    /// <summary>
    /// Turns an absolute path from the report into a root-relative one with forward slashes, or null
    /// when the file lives outside the repository.
    /// </summary>
    private static string? Relativize(string absolutePath, string rootDirectory)
    {
        string normalized = absolutePath.Replace('\\', '/');

        if (string.IsNullOrEmpty(rootDirectory))
            return normalized;

        string root = rootDirectory.Replace('\\', '/').TrimEnd('/') + "/";

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
        string normalized = pattern.Replace('\\', '/').TrimEnd('/');

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
public sealed class CoverageGateOptions
{
    /// <summary>Repository root; report paths are made relative to it.</summary>
    public string RootDirectory { get; init; } = string.Empty;

    /// <summary>Root-relative subtrees the gate covers, e.g. "src". Empty means everything.</summary>
    public IReadOnlyList<string> IncludedPrefixes { get; init; } = [];

    /// <summary>Root-relative files or subtrees exempt from the threshold. Every entry needs a written reason.</summary>
    public IReadOnlyList<string> Exclusions { get; init; } = [];

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

/// <summary>The aggregated verdict.</summary>
public sealed record CoverageReport(
    IReadOnlyList<CoverageFile> Files,
    IReadOnlyList<string> Modules,
    IReadOnlyList<string> MissingModules)
{
    public int MeasurableLines => Files.Sum(static f => f.MeasurableLines);

    public int CoveredLines => Files.Sum(static f => f.CoveredLines);

    public double Rate => MeasurableLines == 0 ? 1d : (double)CoveredLines / MeasurableLines;

    public IReadOnlyList<CoverageFile> IncompleteFiles => Files.Where(static f => !f.IsFullyCovered).ToList();

    /// <summary>True when anything is under the threshold, or when an expected assembly never reported.</summary>
    public bool Failed => IncompleteFiles.Count > 0 || MissingModules.Count > 0;
}
