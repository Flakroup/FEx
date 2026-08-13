using Nuke.Common;
using Nuke.Common.Tooling;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Building;

public interface ITagTarget : INuGetPublishTarget
{
    Target Tag =>
        _ => _.Description("Creates and pushes a Git version tag (e.g. v1.2.3-alpha.4)")
            .TriggeredBy(Publish)
            .After(Publish)
            .OnlyWhenDynamic(() => NukeBuild.IsServerBuild, "Skipping tag: not running on CI")
            .OnlyWhenDynamic(() => ResolveCiRemote() is not null,
                "Skipping tag: no supported CI remote/token (GitHub Actions or GitLab CI)")
            .OnlyWhenDynamic(() => IsReleaseBranch(ResolveCiRemote()?.Branch),
                "Skipping tag: only tags main/master/develop")
            .Executes(() =>
            {
                var remote = ResolveCiRemote()!.Value;
                var tag = $"{TagPrefix}{SemVer}";

                var skip = DescribeTagSkip(tag, GitTags.OnHead(TagPrefix), GitTags.All().Contains(tag));

                if (skip is not null)
                {
                    Log.Information("Skipping tag {Tag}: {Reason}", tag, skip);

                    return;
                }

                Log.Information("Creating Git tag: {Tag}", tag);

                RunGit("config user.email \"ci@flakroup.com\"");
                RunGit("config user.name \"CI\"");
                RunGit($"tag {tag}");
                RunGit($"push {remote.PushUrl} {tag}");

                Log.Information("Successfully pushed tag {Tag}", tag);
            });

    // Resolves the authenticated push URL and current branch for the active CI
    // provider (GitHub Actions or GitLab CI). Returns null when neither is
    // detected or the required token/context is missing. The token is never
    // logged (RunGit uses logInvocation: false).
    static (string? Branch, string PushUrl)? ResolveCiRemote()
    {
        // GitHub Actions
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
        {
            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            var repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY"); // owner/repo

            if (string.IsNullOrEmpty(token)
                || string.IsNullOrEmpty(repository))
                return null;

            var server = Environment.GetEnvironmentVariable("GITHUB_SERVER_URL") ?? "https://github.com";
            var host = new Uri(server).Host;
            var branch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");

            return (branch, $"https://x-access-token:{token}@{host}/{repository}.git");
        }

        // GitLab CI
        if (Environment.GetEnvironmentVariable("GITLAB_CI") == "true")
        {
            var token = Environment.GetEnvironmentVariable("CI_JOB_TOKEN");
            var serverUrl = Environment.GetEnvironmentVariable("CI_SERVER_URL");
            var projectPath = Environment.GetEnvironmentVariable("CI_PROJECT_PATH");

            if (string.IsNullOrEmpty(token)
                || string.IsNullOrEmpty(serverUrl)
                || string.IsNullOrEmpty(projectPath))
                return null;

            var uri = new Uri(serverUrl);
            var branch = Environment.GetEnvironmentVariable("CI_COMMIT_BRANCH");

            return (branch, $"{uri.Scheme}://gitlab-ci-token:{token}@{uri.Host}/{projectPath}.git");
        }

        return null;
    }

    static bool IsReleaseBranch(string? branch) => branch is "main" or "master" or "develop";

    /// <summary>
    /// A commit carries at most one version tag. Checking only whether the tag <em>name</em> is free is not
    /// enough - a re-run that resolves to a different SemVer would find the new name free and stack a second
    /// tag on a commit that is already released.
    /// </summary>
    /// <returns>The reason to skip tagging, or <c>null</c> when the tag may be created.</returns>
    public static string? DescribeTagSkip(string tag, ISet<string> versionTagsOnHead, bool tagNameTaken)
    {
        if (versionTagsOnHead.Contains(tag))
            return "HEAD already carries it";

        if (GitTags.MarksReleasedCommit(versionTagsOnHead))
            return "HEAD already carries version tag(s) "
                   + string.Join(", ", versionTagsOnHead.OrderBy(static t => t, StringComparer.Ordinal));

        return tagNameTaken
            ? "the name is taken by another commit"
            : null;
    }

    static void RunGit(string arguments)
    {
        using var process = ProcessTasks.StartProcess("git",
            arguments,
            NukeBuild.RootDirectory,
            logOutput: false,
            logInvocation: false);

        process.AssertZeroExitCode();

        foreach (var line in process.Output)
            Log.Debug("[git] {Text}", line.Text);
    }
}