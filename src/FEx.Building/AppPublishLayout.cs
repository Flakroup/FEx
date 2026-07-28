using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FEx.Building;

/// <summary>
/// Where <see cref="IAppPublishTarget" /> puts each published application. Separated from the target so the
/// one decision it makes - and the one way it can silently go wrong - is testable without running a build.
/// </summary>
public static class AppPublishLayout
{
    /// <summary>Output folder name for a project: its file name without the extension.</summary>
    public static string OutputName(string projectPath) =>
        Path.GetFileNameWithoutExtension(projectPath)
        ?? throw new ArgumentException($"Cannot derive an output name from '{projectPath}'.", nameof(projectPath));

    /// <summary>
    /// Fails when two projects would publish into the same folder. They can only differ by directory - the
    /// output name comes from the file name - so the second publish would overwrite the first and the build
    /// would still go green, shipping one application where two were expected.
    /// </summary>
    public static void EnsureNoOutputCollision(IEnumerable<string> projectPaths)
    {
        var collisions = projectPaths
            .GroupBy(OutputName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (collisions.Count == 0)
            return;

        var detail = string.Join("; ", collisions.Select(g => $"{g.Key} <- {string.Join(", ", g)}"));
        throw new InvalidOperationException(
            $"PublishProjects would publish into the same folder more than once: {detail}. " +
            "Give the colliding projects distinct file names, or publish them in separate runs.");
    }
}
