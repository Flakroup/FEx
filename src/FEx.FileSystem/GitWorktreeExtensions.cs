using System;
using System.IO;
using System.Linq;

namespace FEx.FileSystem;

public enum GitWorktreeKind
{
    None,
    Worktree,
    Submodule
}

public static class GitWorktreeExtensions
{
    private const string GitDirPrefix = "gitdir: ";
    private const string WorktreesSegment = "/worktrees/";
    private const string ModulesSegment = "/modules/";

    public static GitWorktreeKind GetGitWorktreeKind(this DirectoryInfo dir)
    {
        if (dir is null)
            return GitWorktreeKind.None;

        var gitPath = Path.Combine(dir.FullName, ".git");

        if (Directory.Exists(gitPath))
            return GitWorktreeKind.None;

        if (!File.Exists(gitPath))
            return GitWorktreeKind.None;

        string? firstLine;

        try
        {
            firstLine = File.ReadLines(gitPath).FirstOrDefault();
        }
        catch
        {
            return GitWorktreeKind.None;
        }

        if (string.IsNullOrEmpty(firstLine)
            || !firstLine.StartsWith(GitDirPrefix, StringComparison.Ordinal))
            return GitWorktreeKind.None;

        var target = firstLine.Substring(GitDirPrefix.Length).Trim().Replace('\\', '/');

        // A submodule INSIDE a worktree contains both segments:
        // <main>/.git/worktrees/<wt>/modules/<sub>. The last segment wins -
        // it is the one that directly leads to this directory's .git target.
        var worktreesIdx = target.LastIndexOf(WorktreesSegment, StringComparison.Ordinal);
        var modulesIdx = target.LastIndexOf(ModulesSegment, StringComparison.Ordinal);

        if (worktreesIdx < 0
            && modulesIdx < 0)
            return GitWorktreeKind.None;

        return modulesIdx > worktreesIdx
            ? GitWorktreeKind.Submodule
            : GitWorktreeKind.Worktree;
    }

    public static bool IsGitWorktree(this DirectoryInfo dir) => dir.GetGitWorktreeKind() == GitWorktreeKind.Worktree;
}