using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using FEx.McpServer.Services;
using ModelContextProtocol.Server;

namespace FEx.McpServer.Tools;

[McpServerToolType]
public sealed class FExApiTools
{
    [McpServerTool(Name = "search_fex_api")]
    [Description("Search FEx framework API surface by name, namespace, summary, or method signature. Use before writing new logic to check if FEx already provides the functionality.")]
    public static string SearchApi(
        TomlApiSurfaceReader reader,
        [Description("Search query: class name, method name, interface, namespace, or description")] string query,
        [Description("Filter by project name (e.g. 'Core.Abstractions', 'MVVM')")] string project = "",
        [Description("Filter by type: class, interface, enum, extension")] string type = "")
    {
        var results = reader.Search(query, project, type);

        if (results.Count == 0)
            return $"No results for '{query}'. FEx does not have this functionality - consider adding it if reusable.";

        var sb = new StringBuilder();
        sb.AppendLine($"Found {results.Count} result(s) for '{query}':");
        sb.AppendLine();

        foreach (var entry in results.Take(30))
        {
            var label = entry.Type == ApiEntryType.Constructor && string.IsNullOrEmpty(entry.Name)
                ? entry.Parent
                : entry.Name;
            sb.AppendLine($"**{entry.Type}**: `{label}`");
            if (!string.IsNullOrEmpty(entry.Parent))
                sb.AppendLine($"  Member of: {entry.Parent}");
            if (!string.IsNullOrEmpty(entry.Namespace))
                sb.AppendLine($"  Namespace: {entry.Namespace}");
            sb.AppendLine($"  Project: {entry.Project}");
            if (!string.IsNullOrEmpty(entry.Signature))
                sb.AppendLine($"  Signature: `{entry.Signature}`");
            if (!string.IsNullOrEmpty(entry.Base))
                sb.AppendLine($"  Base: {entry.Base}");
            if (!string.IsNullOrEmpty(entry.Summary))
                sb.AppendLine($"  Summary: {entry.Summary}");
            if (!string.IsNullOrEmpty(entry.File))
                sb.AppendLine($"  File: {entry.File}");
            if (entry.IsStatic)
                sb.AppendLine("  Static: yes");
            sb.AppendLine();
        }

        if (results.Count > 30)
            sb.AppendLine($"... and {results.Count - 30} more. Narrow your query.");

        return sb.ToString();
    }

    [McpServerTool(Name = "list_fex_projects")]
    [Description("List all FEx framework projects with API statistics (classes, interfaces, enums, extension methods).")]
    public static string ListProjects(TomlApiSurfaceReader reader)
    {
        var sb = new StringBuilder();
        var totalC = reader.Entries.Count(e => e.Type == ApiEntryType.Class);
        var totalI = reader.Entries.Count(e => e.Type == ApiEntryType.Interface);
        var totalE = reader.Entries.Count(e => e.Type == ApiEntryType.Enum);
        var totalX = reader.Entries.Count(e => e.Type == ApiEntryType.Extension);

        sb.AppendLine($"FEx Projects ({reader.Projects.Count}):");
        sb.AppendLine($"Total: {reader.Entries.Count} entries ({totalC}C {totalI}I {totalE}E {totalX}X)");
        sb.AppendLine();

        foreach (var p in reader.Projects.OrderBy(p => p.Name))
        {
            var total = p.Classes + p.Interfaces + p.Enums + p.Extensions;
            sb.AppendLine($"  {p.Name} ({total}): {p.Classes}C {p.Interfaces}I {p.Enums}E {p.Extensions}X");
        }

        return sb.ToString();
    }

    [McpServerTool(Name = "get_fex_project_api")]
    [Description("Get complete API surface for a specific FEx project. Returns all classes, interfaces, enums, and extension methods.")]
    public static string GetProjectApi(
        TomlApiSurfaceReader reader,
        [Description("Project name (e.g. 'FEx.Core.Abstractions', 'FEx.MVVM', or just 'Core.Abstractions')")] string project)
    {
        var entries = reader.Entries
            .Where(e => e.Project.Contains(project, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (entries.Count == 0)
            return $"No project matching '{project}'. Use list_fex_projects to see available projects.";

        var actualProject = entries.First().Project;
        var sb = new StringBuilder();
        sb.AppendLine($"API for {actualProject} ({entries.Count} entries):");
        sb.AppendLine();

        foreach (var group in entries.GroupBy(e => e.Type).OrderBy(g => g.Key))
        {
            sb.AppendLine($"## {Plural(group.Key)} ({group.Count()})");

            // Member entries are grouped under their declaring type for readability.
            if (group.First().IsMember)
            {
                foreach (var byType in group.GroupBy(e => e.Parent).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"  {byType.Key}");
                    foreach (var entry in byType.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        var sig = string.IsNullOrEmpty(entry.Signature) ? entry.Name : entry.Signature;
                        sb.Append($"    - `{sig}`");
                        if (!string.IsNullOrEmpty(entry.Summary))
                            sb.Append($" - {entry.Summary}");
                        sb.AppendLine();
                    }
                }
                sb.AppendLine();
                continue;
            }

            foreach (var entry in group.OrderBy(e => e.Name))
            {
                if (entry.Type == ApiEntryType.Extension)
                {
                    sb.Append($"  - `{entry.Signature}`");
                    if (!string.IsNullOrEmpty(entry.Summary))
                        sb.Append($" - {entry.Summary}");
                    sb.AppendLine();
                }
                else
                {
                    sb.Append($"  - `{entry.Name}` ({entry.Namespace})");
                    if (!string.IsNullOrEmpty(entry.Base))
                        sb.Append($" : {entry.Base}");
                    if (!string.IsNullOrEmpty(entry.Summary))
                        sb.Append($" - {entry.Summary}");
                    sb.AppendLine();
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string Plural(ApiEntryType type) => type switch
    {
        ApiEntryType.Class => "Classes",
        ApiEntryType.Interface => "Interfaces",
        ApiEntryType.Enum => "Enums",
        ApiEntryType.Extension => "Extensions",
        ApiEntryType.Constructor => "Constructors",
        ApiEntryType.Method => "Methods",
        ApiEntryType.Property => "Properties",
        _ => type + "s"
    };

    [McpServerTool(Name = "check_fex_freshness")]
    [Description("Check if the FEx API surface TOML files are up to date with the current git HEAD.")]
    public static string CheckFreshness(
        TomlApiSurfaceReader reader,
        ApiSurfaceConfig config)
    {
        var tomlCommit = reader.GitCommit;
        var generated = reader.Generated;

        var headCommit = TryReadGitHeadShort(config.RepoPath);
        if (string.IsNullOrEmpty(headCommit))
            return $"Cannot check git HEAD: failed to read .git/HEAD. TOML commit: {tomlCommit}, generated: {generated}";

        var isFresh = string.Equals(tomlCommit, headCommit, StringComparison.OrdinalIgnoreCase);

        return isFresh
            ? $"API surface is UP TO DATE. Commit: {tomlCommit}, generated: {generated}"
            : $"API surface is OUTDATED. TOML: {tomlCommit}, HEAD: {headCommit}. Run Generate-ApiSurface.ps1 to update.";
    }

    // Reads the short (7-char) SHA for HEAD directly from .git files, avoiding a git subprocess
    // that can hang when the process has inherited pipe handles (e.g. under an MCP stdio host).
    private static string TryReadGitHeadShort(string repoPath)
    {
        try
        {
            var gitDir = Path.Combine(repoPath, ".git");
            var headContent = File.ReadAllText(Path.Combine(gitDir, "HEAD")).Trim();

            string fullSha;

            if (headContent.StartsWith("ref: ", StringComparison.Ordinal))
            {
                var refPath = headContent[5..]; // e.g. "refs/heads/develop"
                var refFile = Path.Combine(gitDir, refPath.Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(refFile))
                {
                    fullSha = File.ReadAllText(refFile).Trim();
                }
                else
                {
                    // Ref has been packed — search packed-refs
                    var packedRefs = Path.Combine(gitDir, "packed-refs");
                    if (!File.Exists(packedRefs))
                        return "";

                    string foundSha = "";
                    foreach (var line in File.ReadLines(packedRefs))
                    {
                        if (line.Length == 0 || line[0] == '#' || line[0] == '^')
                            continue;
                        var space = line.IndexOf(' ');
                        if (space > 0 && line[(space + 1)..] == refPath)
                        {
                            foundSha = line[..space];
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(foundSha))
                        return "";

                    fullSha = foundSha;
                }
            }
            else
            {
                // Detached HEAD — content is already the full SHA
                fullSha = headContent;
            }

            return fullSha.Length >= 7 ? fullSha[..7] : fullSha;
        }
        catch
        {
            return "";
        }
    }
}
