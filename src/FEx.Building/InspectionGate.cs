using System;
using System.Collections.Generic;
using System.Text.Json;

namespace FEx.Building;

/// <summary>
/// Turns an <c>inspectcode</c> SARIF report into the list of findings that decide the build.
/// </summary>
/// <remarks>
/// The report is the verdict and the exit code is not: <c>inspectcode</c> exits 0 whether it found a
/// hundred errors or none, so a target that merely ran it would be a gate that always passes.
/// <para>
/// What counts as a finding is decided by the command line, not here - the run is already narrowed to a
/// minimum severity, so anything that reached the report reached it deliberately. Re-filtering by level in
/// this class would mean two places deciding one thing, and the quieter of the two would win silently.
/// </para>
/// </remarks>
public static class InspectionGate
{
    /// <summary>Every finding in a SARIF report, in the order the tool wrote them. Pure - the unit-test entry point.</summary>
    /// <param name="sarif">The report's text. A byte order mark, present or not, is not allowed to decide anything.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sarif" /> is null.</exception>
    /// <exception cref="JsonException">The report is not JSON - which is what reading it as XML looks like from here.</exception>
    public static IReadOnlyList<InspectionFinding> Analyze(string sarif)
    {
        ArgumentNullException.ThrowIfNull(sarif);

        // The file is SARIF JSON whatever extension it carries, and inspectcode writes it with a BOM often
        // enough that trimming it is cheaper than explaining the parse error it causes. Spelled as an escape
        // rather than as the character itself, which is invisible in every editor that would have to keep it.
        using var document = JsonDocument.Parse(sarif.TrimStart('\uFEFF'));

        if (!document.RootElement.TryGetProperty("runs", out var runs))
            return [];

        // Loops rather than LINQ throughout: JsonElement's enumerators are disposable, and foreach is what
        // disposes them.
        var findings = new List<InspectionFinding>();

        foreach (var run in runs.EnumerateArray())
        {
            if (!run.TryGetProperty("results", out var results))
                continue;

            var levels = RuleLevels(run);

            foreach (var result in results.EnumerateArray())
                findings.Add(Read(result, levels));
        }

        return findings;
    }

    /// <summary>
    /// The severity each rule carries by default, which is where a result's own level comes from when it
    /// does not state one - the usual case for a promoted inspection.
    /// </summary>
    private static Dictionary<string, string> RuleLevels(JsonElement run)
    {
        var levels = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!run.TryGetProperty("tool", out var tool)
            || !tool.TryGetProperty("driver", out var driver)
            || !driver.TryGetProperty("rules", out var rules))
            return levels;

        foreach (var rule in rules.EnumerateArray())
        {
            if (rule.TryGetProperty("id", out var id)
                && rule.TryGetProperty("defaultConfiguration", out var configuration)
                && configuration.TryGetProperty("level", out var level))
                levels[id.GetString() ?? string.Empty] = level.GetString() ?? string.Empty;
        }

        return levels;
    }

    private static InspectionFinding Read(JsonElement result, Dictionary<string, string> ruleLevels)
    {
        var ruleId = Text(result, "ruleId");

        var physical = default(JsonElement);

        if (result.TryGetProperty("locations", out var locations))
            foreach (var location in locations.EnumerateArray())
            {
                // The first location is the one a person is sent to; a finding with several is still one
                // finding, and listing them all would turn a report into a count nobody trusts.
                if (location.TryGetProperty("physicalLocation", out var found))
                    physical = found;

                break;
            }

        return new InspectionFinding(
            ruleId,
            Level(result, ruleId, ruleLevels),
            physical.ValueKind == JsonValueKind.Object && physical.TryGetProperty("artifactLocation", out var artifact)
                ? Text(artifact, "uri")
                : string.Empty,
            physical.ValueKind == JsonValueKind.Object
            && physical.TryGetProperty("region", out var region)
            && region.TryGetProperty("startLine", out var line)
                ? line.GetInt32()
                : 0,
            result.TryGetProperty("message", out var message) ? Text(message, "text") : string.Empty);
    }

    private static string Level(JsonElement result, string ruleId, Dictionary<string, string> ruleLevels)
    {
        var own = Text(result, "level");

        return own.Length > 0 ? own
            : ruleLevels.TryGetValue(ruleId, out var declared) ? declared
            : string.Empty;
    }

    private static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}

/// <summary>One inspection result, flattened to what a person needs in order to go and fix it.</summary>
/// <param name="RuleId">The inspection's own name, which is what a suppression would have to name.</param>
/// <param name="Level">Empty when neither the result nor its rule declares one.</param>
/// <param name="File">Repository-relative, or empty for a finding the tool did not place in a file.</param>
/// <param name="Line">0 when the finding carries no line.</param>
/// <param name="Message">The tool's own wording.</param>
public sealed record InspectionFinding(string RuleId, string Level, string File, int Line, string Message)
{
    /// <summary>One line naming where to go, in the shape an editor and a terminal both make clickable.</summary>
    public override string ToString() =>
        (File.Length > 0 ? $"{File}:{Line}" : "<solution>") + $" {RuleId}: {Message}";
}
