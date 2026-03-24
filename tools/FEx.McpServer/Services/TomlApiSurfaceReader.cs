using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace FEx.McpServer.Services;

public sealed class TomlApiSurfaceReader
{
    private readonly List<ApiEntry> _entries = new();
    private readonly List<ProjectInfo> _projects = new();
    private readonly ILogger<TomlApiSurfaceReader> _logger;

    public TomlApiSurfaceReader(ApiSurfaceConfig config, ILogger<TomlApiSurfaceReader> logger)
    {
        _logger = logger;
        LoadAll(config.ApiSurfacePath);
    }

    public IReadOnlyList<ApiEntry> Entries => _entries;
    public IReadOnlyList<ProjectInfo> Projects => _projects;
    public string GitCommit { get; private set; } = "";
    public string Generated { get; private set; } = "";

    private void LoadAll(string dir)
    {
        if (!Directory.Exists(dir))
            return;

        var metaPath = Path.Combine(dir, "_meta.toml");
        if (File.Exists(metaPath))
            LoadMeta(metaPath);

        foreach (var file in Directory.GetFiles(dir, "*.toml"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName == "_meta")
                continue;

            try
            {
                LoadProjectToml(file, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse {File}", file);
            }
        }

        _logger.LogInformation(
            "Loaded {EntryCount} entries from {ProjectCount} projects (commit: {Commit})",
            _entries.Count, _projects.Count, GitCommit);
    }

    private void LoadMeta(string path)
    {
        var sections = ParseToml(File.ReadAllText(path));

        if (sections.TryGetValue("meta", out var metaEntries) && metaEntries.Count > 0)
        {
            var meta = metaEntries[0];
            GitCommit = GetValue(meta, "git_commit");
            Generated = GetValue(meta, "generated");
        }
    }

    private void LoadProjectToml(string filePath, string fileName)
    {
        var sections = ParseToml(File.ReadAllText(filePath));

        var projectName = "FEx." + fileName;
        var projectPath = "";

        if (sections.TryGetValue("project", out var projEntries) && projEntries.Count > 0)
        {
            var proj = projEntries[0];
            projectName = GetValue(proj, "name", projectName);
            projectPath = GetValue(proj, "path");
        }

        int classCount = LoadEntries(sections, "classes", ApiEntryType.Class, projectName);
        int ifaceCount = LoadEntries(sections, "interfaces", ApiEntryType.Interface, projectName);
        int enumCount = LoadEntries(sections, "enums", ApiEntryType.Enum, projectName);
        int extCount = LoadEntries(sections, "extensions", ApiEntryType.Extension, projectName);

        _projects.Add(new ProjectInfo
        {
            Name = projectName,
            Path = projectPath,
            Classes = classCount,
            Interfaces = ifaceCount,
            Enums = enumCount,
            Extensions = extCount
        });
    }

    private int LoadEntries(
        Dictionary<string, List<Dictionary<string, string>>> sections,
        string key,
        ApiEntryType type,
        string project)
    {
        if (!sections.TryGetValue(key, out var entries))
            return 0;

        foreach (var item in entries)
        {
            _entries.Add(new ApiEntry
            {
                Type = type,
                Name = GetValue(item, "name"),
                Namespace = GetValue(item, "ns"),
                Project = project,
                File = GetValue(item, "file"),
                Summary = GetValue(item, "summary"),
                Signature = GetValue(item, "sig"),
                Returns = GetValue(item, "returns"),
                Base = GetValue(item, "base"),
                IsStatic = GetValue(item, "static") == "true"
            });
        }

        return entries.Count;
    }

    public List<ApiEntry> Search(string query, string project, string type)
    {
        var results = _entries.AsEnumerable();

        if (!string.IsNullOrEmpty(project))
            results = results.Where(e => e.Project.Contains(project, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<ApiEntryType>(type, ignoreCase: true, out var t))
            results = results.Where(e => e.Type == t);

        return results
            .Where(e =>
                e.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.Namespace.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.Summary.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.Signature.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Name.Equals(query, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(e => e.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .ThenBy(e => e.Name)
            .ToList();
    }

    private static string GetValue(Dictionary<string, string> dict, string key, string fallback = "")
    {
        return dict.TryGetValue(key, out var val) ? val : fallback;
    }

    private static Dictionary<string, List<Dictionary<string, string>>> ParseToml(string content)
    {
        var sections = new Dictionary<string, List<Dictionary<string, string>>>();
        var currentSection = "_root";
        Dictionary<string, string> currentTable = new();

        sections[currentSection] = new List<Dictionary<string, string>> { currentTable };

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();

            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            var arrayMatch = Regex.Match(line, @"^\[\[(\w+)\]\]$");
            if (arrayMatch.Success)
            {
                currentSection = arrayMatch.Groups[1].Value;
                currentTable = new Dictionary<string, string>();

                if (!sections.ContainsKey(currentSection))
                    sections[currentSection] = new List<Dictionary<string, string>>();

                sections[currentSection].Add(currentTable);
                continue;
            }

            var tableMatch = Regex.Match(line, @"^\[(\w+)\]$");
            if (tableMatch.Success)
            {
                currentSection = tableMatch.Groups[1].Value;
                currentTable = new Dictionary<string, string>();

                if (!sections.ContainsKey(currentSection))
                    sections[currentSection] = new List<Dictionary<string, string>>();

                sections[currentSection].Add(currentTable);
                continue;
            }

            var kvMatch = Regex.Match(line, @"^(\w+)\s*=\s*(.+)$");
            if (kvMatch.Success)
            {
                var key = kvMatch.Groups[1].Value;
                var value = kvMatch.Groups[2].Value.Trim();

                if (value.StartsWith('"') && value.EndsWith('"'))
                    value = value[1..^1].Replace("\\\"", "\"").Replace("\\\\", "\\");

                currentTable[key] = value;
            }
        }

        return sections;
    }
}
