using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FEx.Building;

public interface IGitVersionComponent : INukeBuild
{
    [Parameter("GitVersion output (auto-resolved)", Name = "GitVersionInfo")]
    sealed GitVersionInfo? VersionInfo => TryGetValue(() => VersionInfo) ?? ResolveGitVersion();

    // Lives here rather than on ITagTarget because everything that treats a version tag as "already
    // released" - resolving the version, publishing, tagging - has to read the same prefix. Split across
    // two declarations, a non-default prefix would leave the publish gate blind to the tags it creates.
    [Parameter("Git tag prefix (default: v)")]
    string TagPrefix => TryGetValue(() => TagPrefix) ?? "v";

    sealed string SemVer =>
        !string.IsNullOrEmpty(VersionInfo?.SemVer)
            ? VersionInfo!.SemVer
            : "0.0.0";

    sealed string NuGetVersion =>
        !string.IsNullOrEmpty(VersionInfo?.NuGetVersionV2) ? VersionInfo!.NuGetVersionV2 :
        !string.IsNullOrEmpty(VersionInfo?.NuGetVersion) ? VersionInfo!.NuGetVersion :
        !string.IsNullOrEmpty(VersionInfo?.SemVer) ? VersionInfo!.SemVer : "0.0.0";

    sealed string InformationalVersion =>
        !string.IsNullOrEmpty(VersionInfo?.InformationalVersion)
            ? VersionInfo!.InformationalVersion
            : "0.0.0";

    sealed GitVersionInfo ResolveGitVersion()
    {
        var toolPath = NuGetToolPathResolver.GetPackageExecutable("GitVersion.Tool", "gitversion.dll|gitversion.exe");

        Log.Information("GitVersion tool path: {ToolPath}", toolPath);

        const string arguments = "/output json /nofetch";
        IReadOnlyCollection<Output> output;

        var toolIsDll = toolPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

        var executable = toolIsDll
            ? "dotnet"
            : toolPath;

        var args = toolIsDll
            ? $"\"{toolPath}\" {arguments}"
            : arguments;

        using var process = ProcessTasks.StartProcess(
            executable,
            args,
            RootDirectory,
            logOutput: false,
            logInvocation: false);

        process.AssertZeroExitCode();
        output = process.Output;

        var json = output.Where(o => o.Type == OutputType.Std).Select(o => o.Text).JoinNewLine();

        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new LenientStringConverter());

        var info = JsonSerializer.Deserialize<GitVersionInfo>(json, jsonOptions)
                   ?? throw new InvalidOperationException("GitVersion returned empty output");

        Log.Information("GitVersion: SemVer={SemVer} NuGet={NuGetVersion} Branch={Branch}",
            info.SemVer,
            info.NuGetVersionV2,
            info.BranchName);

        if (NukeBuild.IsServerBuild)
            AssertVersionAvailable(info, TagPrefix, GitTags.All(), GitTags.OnHead(TagPrefix));

        return info;
    }

    /// <summary>
    /// A version tag names exactly one commit. On HEAD it means this commit is already released, which the
    /// publish and tag steps recognise and skip on. On any other commit it means the tags or the history are
    /// wrong - a stale manual tag, a rewritten history - and inventing a fresh version to dodge the collision
    /// would ship releases nobody planned, so the build stops for a human instead.
    /// </summary>
    /// <exception cref="InvalidOperationException">The version's tag is taken by a different commit.</exception>
    public static void AssertVersionAvailable(GitVersionInfo info,
                                              string tagPrefix,
                                              ISet<string> allTags,
                                              ISet<string> tagsOnHead)
    {
        var tag = $"{tagPrefix}{info.SemVer}";

        if (tagsOnHead.Contains(tag))
        {
            Log.Information("Tag {Tag} is on HEAD - this commit is already released, keeping its version", tag);

            return;
        }

        if (!allTags.Contains(tag))
            return;

        throw new InvalidOperationException(
            $"Version {info.SemVer} resolves to tag {tag}, which is already on a different commit. "
            + "Publishing it would release the same version twice. Delete the stale tag, or fix the history "
            + "that made two commits resolve to one version.");
    }
}

public sealed record GitVersionInfo
{
    [JsonPropertyName("Major")]
    public int Major { get; init; }

    [JsonPropertyName("Minor")]
    public int Minor { get; init; }

    [JsonPropertyName("Patch")]
    public int Patch { get; init; }

    [JsonPropertyName("PreReleaseTag")]
    public string PreReleaseTag { get; init; } = "";

    [JsonPropertyName("PreReleaseTagWithDash")]
    public string PreReleaseTagWithDash { get; init; } = "";

    [JsonPropertyName("PreReleaseLabel")]
    public string PreReleaseLabel { get; init; } = "";

    [JsonPropertyName("PreReleaseLabelWithDash")]
    public string PreReleaseLabelWithDash { get; init; } = "";

    [JsonPropertyName("PreReleaseNumber")]
    public int? PreReleaseNumber { get; init; }

    [JsonPropertyName("WeightedPreReleaseNumber")]
    public int? WeightedPreReleaseNumber { get; init; }

    [JsonPropertyName("BuildMetaData")]
    public string BuildMetaData { get; init; } = "";

    [JsonPropertyName("FullBuildMetaData")]
    public string FullBuildMetaData { get; init; } = "";

    [JsonPropertyName("MajorMinorPatch")]
    public string MajorMinorPatch { get; init; } = "";

    [JsonPropertyName("SemVer")]
    public string SemVer { get; init; } = "";

    [JsonPropertyName("AssemblySemVer")]
    public string AssemblySemVer { get; init; } = "";

    [JsonPropertyName("AssemblySemFileVer")]
    public string AssemblySemFileVer { get; init; } = "";

    [JsonPropertyName("FullSemVer")]
    public string FullSemVer { get; init; } = "";

    [JsonPropertyName("InformationalVersion")]
    public string InformationalVersion { get; init; } = "";

    [JsonPropertyName("BranchName")]
    public string BranchName { get; init; } = "";

    [JsonPropertyName("EscapedBranchName")]
    public string EscapedBranchName { get; init; } = "";

    [JsonPropertyName("Sha")]
    public string Sha { get; init; } = "";

    [JsonPropertyName("ShortSha")]
    public string ShortSha { get; init; } = "";

    [JsonPropertyName("NuGetVersionV2")]
    public string NuGetVersionV2 { get; init; } = "";

    [JsonPropertyName("NuGetVersion")]
    public string NuGetVersion { get; init; } = "";

    [JsonPropertyName("NuGetPreReleaseTag")]
    public string NuGetPreReleaseTag { get; init; } = "";

    [JsonPropertyName("NuGetPreReleaseTagV2")]
    public string NuGetPreReleaseTagV2 { get; init; } = "";

    [JsonPropertyName("VersionSourceSha")]
    public string VersionSourceSha { get; init; } = "";

    [JsonPropertyName("CommitsSinceVersionSource")]
    public int CommitsSinceVersionSource { get; init; }

    [JsonPropertyName("CommitDate")]
    public string CommitDate { get; init; } = "";

    [JsonPropertyName("UncommittedChanges")]
    public int UncommittedChanges { get; init; }
}

internal sealed class LenientStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number
            ? reader.TryGetInt64(out var n)
                ? n.ToString()
                : reader.GetDouble().ToString(CultureInfo.InvariantCulture)
            : reader.GetString() ?? "";

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}