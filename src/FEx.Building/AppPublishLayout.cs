using Nuke.Common.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FEx.Building;

/// <summary>
/// One application to publish: the project to build and the directory its output lands in. Separate
/// entries never share a directory - see <see cref="AppPublishLayout.EnsureNoOutputCollision" />.
/// </summary>
public sealed record AppPublishEntry(AbsolutePath ProjectPath, AbsolutePath OutputDirectory);

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
    /// Fails when two entries would publish into the same directory. The second publish would overwrite the
    /// first and the build would still go green, shipping one application where two were expected. Checked
    /// on the directory rather than the project name because an entry may name its own output directory -
    /// two projects with distinct names can still be pointed at one folder.
    /// </summary>
    public static void EnsureNoOutputCollision(IEnumerable<AppPublishEntry> entries)
    {
        var collisions = entries
            .GroupBy(static entry => entry.OutputDirectory.ToString(), StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1)
            .ToList();

        if (collisions.Count == 0)
            return;

        var detail = string.Join("; ",
            collisions.Select(static group =>
                $"{group.Key} <- {string.Join(", ", group.Select(static entry => entry.ProjectPath))}"));

        throw new InvalidOperationException(
            $"PublishEntries would publish into the same directory more than once: {detail}. "
            + "Give the colliding projects distinct output directories, or publish them in separate runs.");
    }
}