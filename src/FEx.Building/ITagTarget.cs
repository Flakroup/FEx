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
        .OnlyWhenDynamic(() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI_JOB_TOKEN")),
            "Skipping tag: no CI_JOB_TOKEN")
        .Executes(() =>
        {
            var tag = $"{TagPrefix}{SemVer}";

            Log.Information("Creating Git tag: {Tag}", tag);

            var serverUrl = Environment.GetEnvironmentVariable("CI_SERVER_URL");
            var projectPath = Environment.GetEnvironmentVariable("CI_PROJECT_PATH");
            var jobToken = Environment.GetEnvironmentVariable("CI_JOB_TOKEN");

            if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(projectPath))
            {
                Log.Warning("CI_SERVER_URL or CI_PROJECT_PATH not set - cannot push tag");
                return;
            }

            var host = new Uri(serverUrl).Host;
            var scheme = new Uri(serverUrl).Scheme;
            var authenticatedUrl = $"{scheme}://gitlab-ci-token:{jobToken}@{host}/{projectPath}.git";

            if (TagExists(tag))
            {
                Log.Information("Tag {Tag} already exists - skipping", tag);
                return;
            }

            RunGit($"tag {tag}");
            RunGit($"push {authenticatedUrl} {tag}");

            Log.Information("Successfully pushed tag {Tag}", tag);
        });

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
