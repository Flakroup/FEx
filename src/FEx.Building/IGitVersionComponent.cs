using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Serilog;

namespace FEx.Building;

public interface IGitVersionComponent : INukeBuild
{
    [Parameter("GitVersion output (auto-resolved)", Name = "GitVersionInfo")]
    sealed GitVersionInfo? VersionInfo => TryGetValue(() => VersionInfo) ?? ResolveGitVersion();

    sealed string SemVer => !string.IsNullOrEmpty(VersionInfo?.SemVer) ? VersionInfo!.SemVer : "0.0.0";

    sealed string NuGetVersion => !string.IsNullOrEmpty(VersionInfo?.NuGetVersionV2) ? VersionInfo!.NuGetVersionV2
                               : !string.IsNullOrEmpty(VersionInfo?.NuGetVersion) ? VersionInfo!.NuGetVersion
                               : !string.IsNullOrEmpty(VersionInfo?.SemVer) ? VersionInfo!.SemVer
                               : "0.0.0";

    sealed string InformationalVersion => !string.IsNullOrEmpty(VersionInfo?.InformationalVersion) ? VersionInfo!.InformationalVersion : "0.0.0";

    sealed GitVersionInfo ResolveGitVersion()
    {
        var toolPath = NuGetToolPathResolver.GetPackageExecutable(
            packageId: "GitVersion.Tool",
            packageExecutable: "gitversion.dll|gitversion.exe");

        Log.Information("GitVersion tool path: {ToolPath}", toolPath);

        const string arguments = "/output json /nofetch";
        IReadOnlyCollection<Output> output;

        var toolIsDll = toolPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
        var executable = toolIsDll ? "dotnet" : toolPath;
        var args = toolIsDll ? $"\"{toolPath}\" {arguments}" : arguments;

        using var process = ProcessTasks.StartProcess(
            executable,
            args,
            RootDirectory,
            logOutput: false,
            logInvocation: false);

        process.AssertZeroExitCode();
        output = process.Output;

        var json = output
            .Where(o => o.Type == OutputType.Std)
            .Select(o => o.Text)
            .JoinNewLine();

        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new LenientStringConverter());

        var info = JsonSerializer.Deserialize<GitVersionInfo>(json, jsonOptions)
                   ?? throw new InvalidOperationException("GitVersion returned empty output");

        Log.Information("GitVersion: SemVer={SemVer} NuGet={NuGetVersion} Branch={Branch}",
            info.SemVer, info.NuGetVersionV2, info.BranchName);

        if (NukeBuild.IsServerBuild)
            info = BumpIfTagExists(info);

        return info;
    }

    private static GitVersionInfo BumpIfTagExists(GitVersionInfo info)
    {
        var existingTags = GetExistingTags();
        var candidate = info;

        while (existingTags.Contains($"v{candidate.SemVer}"))
        {
            var oldVersion = candidate.SemVer;
            candidate = candidate.WithPatch(candidate.Patch + 1);
            Log.Warning("Tag v{OldVersion} already exists - bumping to {NewVersion}",
                oldVersion, candidate.SemVer);
        }

        return candidate;
    }

    private static HashSet<string> GetExistingTags()
    {
        using var process = ProcessTasks.StartProcess(
            "git", "tag -l",
            NukeBuild.RootDirectory,
            logOutput: false,
            logInvocation: false);

        process.WaitForExit();

        return process.Output
            .Select(static line => line.Text.Trim())
            .Where(static t => t.Length > 0)
            .ToHashSet();
    }
}

public sealed record GitVersionInfo
{
    [JsonPropertyName("Major")] public int Major { get; init; }
    [JsonPropertyName("Minor")] public int Minor { get; init; }
    [JsonPropertyName("Patch")] public int Patch { get; init; }
    [JsonPropertyName("PreReleaseTag")] public string PreReleaseTag { get; init; } = "";
    [JsonPropertyName("PreReleaseTagWithDash")] public string PreReleaseTagWithDash { get; init; } = "";
    [JsonPropertyName("PreReleaseLabel")] public string PreReleaseLabel { get; init; } = "";
    [JsonPropertyName("PreReleaseLabelWithDash")] public string PreReleaseLabelWithDash { get; init; } = "";
    [JsonPropertyName("PreReleaseNumber")] public int? PreReleaseNumber { get; init; }
    [JsonPropertyName("WeightedPreReleaseNumber")] public int? WeightedPreReleaseNumber { get; init; }
    [JsonPropertyName("BuildMetaData")] public string BuildMetaData { get; init; } = "";
    [JsonPropertyName("FullBuildMetaData")] public string FullBuildMetaData { get; init; } = "";
    [JsonPropertyName("MajorMinorPatch")] public string MajorMinorPatch { get; init; } = "";
    [JsonPropertyName("SemVer")] public string SemVer { get; init; } = "";
    [JsonPropertyName("AssemblySemVer")] public string AssemblySemVer { get; init; } = "";
    [JsonPropertyName("AssemblySemFileVer")] public string AssemblySemFileVer { get; init; } = "";
    [JsonPropertyName("FullSemVer")] public string FullSemVer { get; init; } = "";
    [JsonPropertyName("InformationalVersion")] public string InformationalVersion { get; init; } = "";
    [JsonPropertyName("BranchName")] public string BranchName { get; init; } = "";
    [JsonPropertyName("EscapedBranchName")] public string EscapedBranchName { get; init; } = "";
    [JsonPropertyName("Sha")] public string Sha { get; init; } = "";
    [JsonPropertyName("ShortSha")] public string ShortSha { get; init; } = "";
    [JsonPropertyName("NuGetVersionV2")] public string NuGetVersionV2 { get; init; } = "";
    [JsonPropertyName("NuGetVersion")] public string NuGetVersion { get; init; } = "";
    [JsonPropertyName("NuGetPreReleaseTag")] public string NuGetPreReleaseTag { get; init; } = "";
    [JsonPropertyName("NuGetPreReleaseTagV2")] public string NuGetPreReleaseTagV2 { get; init; } = "";
    [JsonPropertyName("VersionSourceSha")] public string VersionSourceSha { get; init; } = "";
    [JsonPropertyName("CommitsSinceVersionSource")] public int CommitsSinceVersionSource { get; init; }
    [JsonPropertyName("CommitDate")] public string CommitDate { get; init; } = "";
    [JsonPropertyName("UncommittedChanges")] public int UncommittedChanges { get; init; }

    public GitVersionInfo WithPatch(int newPatch)
    {
        var mmp = $"{Major}.{Minor}.{newPatch}";
        var semver = string.IsNullOrEmpty(PreReleaseTagWithDash) ? mmp : $"{mmp}{PreReleaseTagWithDash}";
        var nuget = string.IsNullOrEmpty(NuGetPreReleaseTagV2) ? mmp : $"{mmp}-{NuGetPreReleaseTagV2}";

        return this with
        {
            Patch = newPatch,
            MajorMinorPatch = mmp,
            SemVer = semver,
            FullSemVer = semver,
            NuGetVersionV2 = nuget,
            NuGetVersion = nuget,
            AssemblySemVer = $"{mmp}.0",
            AssemblySemFileVer = $"{mmp}.0",
            InformationalVersion = $"{semver}+Branch.{BranchName}.Sha.{Sha}",
        };
    }
}

internal sealed class LenientStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number
            ? (reader.TryGetInt64(out var n) ? n.ToString() : reader.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture))
            : reader.GetString() ?? "";

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
