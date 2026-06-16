using Shouldly;
using System;
using System.IO;
using Xunit;

namespace FEx.FileSystem.Tests;

public sealed class GitWorktreeExtensionsTests
{
    [Fact]
    public void GetGitWorktreeKind_NullDirectory_ReturnsNone()
    {
        DirectoryInfo dir = null;

        var result = dir.GetGitWorktreeKind();

        result.ShouldBe(GitWorktreeKind.None);
    }

    [Fact]
    public void GetGitWorktreeKind_PlainDirectory_NoGitMarker_ReturnsNone()
    {
        var dir = CreateTempDir();

        try
        {
            var result = new DirectoryInfo(dir.FullName).GetGitWorktreeKind();

            result.ShouldBe(GitWorktreeKind.None);
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Fact]
    public void GetGitWorktreeKind_MainRepo_GitAsFolder_ReturnsNone()
    {
        var dir = CreateTempDir();

        try
        {
            dir.CreateSubdirectory(".git");

            var result = new DirectoryInfo(dir.FullName).GetGitWorktreeKind();

            result.ShouldBe(GitWorktreeKind.None);
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Fact]
    public void GetGitWorktreeKind_LinkedWorktree_RelativeGitdir_ReturnsWorktree()
    {
        var dir = CreateTempDir();

        try
        {
            WriteGitFile(dir, "gitdir: ../.git/worktrees/feature-x");

            var result = new DirectoryInfo(dir.FullName).GetGitWorktreeKind();

            result.ShouldBe(GitWorktreeKind.Worktree);
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Fact]
    public void GetGitWorktreeKind_LinkedWorktree_AbsoluteGitdir_ReturnsWorktree()
    {
        var dir = CreateTempDir();

        try
        {
            WriteGitFile(dir, "gitdir: /tmp/fakemain/.git/worktrees/X");

            var result = new DirectoryInfo(dir.FullName).GetGitWorktreeKind();

            result.ShouldBe(GitWorktreeKind.Worktree);
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Fact]
    public void GetGitWorktreeKind_LinkedWorktree_WindowsBackslashes_ReturnsWorktree()
    {
        var dir = CreateTempDir();

        try
        {
            WriteGitFile(dir, @"gitdir: ..\.git\worktrees\feature-x");

            var result = new DirectoryInfo(dir.FullName).GetGitWorktreeKind();

            result.ShouldBe(GitWorktreeKind.Worktree);
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Fact]
    public void GetGitWorktreeKind_Submodule_ReturnsSubmodule()
    {
        var dir = CreateTempDir();

        try
        {
            WriteGitFile(dir, "gitdir: ../.git/modules/sub");

            var result = new DirectoryInfo(dir.FullName).GetGitWorktreeKind();

            result.ShouldBe(GitWorktreeKind.Submodule);
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Fact]
    public void GetGitWorktreeKind_SubmoduleInsideWorktree_ReturnsSubmodule()
    {
        var dir = CreateTempDir();

        try
        {
            // gitdir contains both /worktrees/ and /modules/. The LAST one (modules) wins.
            WriteGitFile(dir, "gitdir: /home/user/repo/.git/worktrees/wt-feature/modules/sub");

            var result = new DirectoryInfo(dir.FullName).GetGitWorktreeKind();

            result.ShouldBe(GitWorktreeKind.Submodule);
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Fact]
    public void GetGitWorktreeKind_MalformedGitFile_NoPrefix_ReturnsNone()
    {
        var dir = CreateTempDir();

        try
        {
            WriteGitFile(dir, "ref: refs/heads/main");

            var result = new DirectoryInfo(dir.FullName).GetGitWorktreeKind();

            result.ShouldBe(GitWorktreeKind.None);
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Fact]
    public void GetGitWorktreeKind_MultilineGitFile_FirstLineDetermines()
    {
        var dir = CreateTempDir();

        try
        {
            WriteGitFile(dir, "gitdir: ../.git/worktrees/feature-x\nsome other line\nmodules/decoy");

            var result = new DirectoryInfo(dir.FullName).GetGitWorktreeKind();

            result.ShouldBe(GitWorktreeKind.Worktree);
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Fact]
    public void GetGitWorktreeKind_GitdirWithoutWorktreesOrModules_ReturnsNone()
    {
        var dir = CreateTempDir();

        try
        {
            WriteGitFile(dir, "gitdir: /some/unrelated/path");

            var result = new DirectoryInfo(dir.FullName).GetGitWorktreeKind();

            result.ShouldBe(GitWorktreeKind.None);
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Fact]
    public void IsGitWorktree_OnLinkedWorktree_ReturnsTrue()
    {
        var dir = CreateTempDir();

        try
        {
            WriteGitFile(dir, "gitdir: ../.git/worktrees/X");

            new DirectoryInfo(dir.FullName).IsGitWorktree().ShouldBeTrue();
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Fact]
    public void IsGitWorktree_OnSubmodule_ReturnsFalse()
    {
        var dir = CreateTempDir();

        try
        {
            WriteGitFile(dir, "gitdir: ../.git/modules/X");

            new DirectoryInfo(dir.FullName).IsGitWorktree().ShouldBeFalse();
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    private static DirectoryInfo CreateTempDir() =>
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"FExWorktreeTest_{Guid.NewGuid()}"));

    private static void CleanupTempDir(DirectoryInfo dir)
    {
        if (dir.Exists)
            dir.Delete(true);
    }

    private static void WriteGitFile(DirectoryInfo dir, string content) =>
        File.WriteAllText(Path.Combine(dir.FullName, ".git"), content);
}