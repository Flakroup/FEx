using Nuke.Common;
using Nuke.Common.Tooling;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Building;

/// <summary>
/// Reads the repository's version tags. The version tag on a commit is the marker that it has already
/// been published, so every step with an outward-facing effect - pushing packages, creating the tag,
/// resolving the version itself - consults it to stay idempotent across re-runs.
/// </summary>
public static class GitTags
{
    /// <summary>Every tag in the repository.</summary>
    public static ISet<string> All() => Lines("tag -l").ToHashSet();

    /// <summary>Version tags (those under <paramref name="prefix" />) carried by the current HEAD.</summary>
    public static ISet<string> OnHead(string prefix) =>
        Lines($"tag --points-at HEAD --list {prefix}*").ToHashSet();

    /// <summary>
    /// Whether the version tags on a commit mark it as already released. The single source of truth for
    /// that question - the publish gate and the tag gate both ask it here, so they cannot drift apart and
    /// start disagreeing about whether a commit has shipped.
    /// </summary>
    public static bool MarksReleasedCommit(ISet<string> versionTagsOnHead) => versionTagsOnHead.Count > 0;

    // Reads git output as trimmed, non-empty lines. Asserts the exit code: a git failure swallowed into
    // an empty set would read as "nothing tagged yet", which is exactly the state that re-publishes.
    private static IEnumerable<string> Lines(string arguments)
    {
        using var process = ProcessTasks.StartProcess("git",
            arguments,
            NukeBuild.RootDirectory,
            logOutput: false,
            logInvocation: false);

        process.AssertZeroExitCode();

        return process.Output
            .Select(static line => line.Text.Trim())
            .Where(static text => text.Length > 0)
            .ToArray();
    }
}
