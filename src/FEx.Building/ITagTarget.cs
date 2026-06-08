using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Tooling;
using Serilog;

namespace FEx.Building;

public interface ITagTarget : INuGetPublishTarget
{
    [Parameter("Git tag prefix (default: v)")]
    string TagPrefix => TryGetValue(() => TagPrefix) ?? "v";

    Target Tag => _ => _
        .Description("Creates and pushes a Git version tag (e.g. v1.2.3-alpha.4)")
        .TriggeredBy(Publish)
        .After(Publish)
        .OnlyWhenDynamic(() => NukeBuild.IsServerBuild,
            "Skipping tag: not running on CI")
        .OnlyWhenDynamic(() => ResolveCiRemote() is not null,
            "Skipping tag: no supported CI remote/token (GitHub Actions or GitLab CI)")
        .OnlyWhenDynamic(() => IsReleaseBranch(ResolveCiRemote()?.Branch),
            "Skipping tag: only tags main/master/develop")
        .Executes(() =>
        {
            var remote = ResolveCiRemote()!.Value;
            var tag = $"{TagPrefix}{SemVer}";

            if (TagExists(tag))
            {
                Log.Information("Tag {Tag} already exists - skipping", tag);
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
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(repository))
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
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(projectPath))
                return null;

            var uri = new Uri(serverUrl);
            var branch = Environment.GetEnvironmentVariable("CI_COMMIT_BRANCH");

            return (branch, $"{uri.Scheme}://gitlab-ci-token:{token}@{uri.Host}/{projectPath}.git");
        }

        return null;
    }

    static bool IsReleaseBranch(string? branch) => branch is "main" or "master" or "develop";

    static bool TagExists(string tag)
    {
        using var process = ProcessTasks.StartProcess(
            "git",
            $"tag -l {tag}",
            NukeBuild.RootDirectory,
            logOutput: false,
            logInvocation: false);

        process.WaitForExit();

        return process.Output.Any(line => line.Text.Trim() == tag);
    }

    static void RunGit(string arguments)
    {
        using var process = ProcessTasks.StartProcess(
            "git",
            arguments,
            NukeBuild.RootDirectory,
            logOutput: false,
            logInvocation: false);

        process.AssertZeroExitCode();

        foreach (var line in process.Output)
            Log.Debug("[git] {Text}", line.Text);
    }
}
