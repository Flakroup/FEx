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
    // Resolved once per process. GitVersion is an external process costing a second or more, and the
    // version is read from several places - SemVer alone reads this property twice, so an unmemoised
    // getter launched the tool six times in a single publish run (measured, CI run 30262574981).
    private static GitVersionInfo? _resolved;

    // Deliberately NOT a [Parameter]. The version comes from git history and nowhere else: the attribute
    // only ever existed so TryGetValue could read this property back through NUKE's ParameterService, and
    // what it bought was a supply route nobody uses and that mostly does not work - NUKE's argument parser
    // is string-to-scalar, so --git-version-info and the environment variable both fail to build the
    // record, leaving a .nuke parameters file as the one way in. Keeping it published a switch that
    // cannot be honoured and let an injected value diverge from what IsVersionResolved reported.
    sealed GitVersionInfo? VersionInfo => _resolved ??= ResolveGitVersion();

    // Lives here rather than on ITagTarget because everything that treats a version tag as "already
    // released" - resolving the version, publishing, tagging - has to read the same prefix. Split across
    // two declarations, a non-default prefix would leave the publish gate blind to the tags it creates.
    [Parameter("Git tag prefix (default: v)")]
    string TagPrefix => TryGetValue(() => TagPrefix) ?? "v";

    sealed string SemVer =>
        !string.IsNullOrEmpty(VersionInfo?.SemVer)
            ? VersionInfo!.SemVer
            : "0.0.0";

    // SemVer is the package version. GitVersion 6 no longer emits the NuGetVersion* variables, so a
    // fallback chain reading them resolved to empty and handed the packages a version with the
    // pre-release label silently stripped - which is how a develop snapshot shipped as a stable release.
    sealed string NuGetVersion => SemVer;

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

        var json = output.Where(static o => o.Type == OutputType.Std).Select(static o => o.Text).JoinNewLine();

        var info = Parse(json);

        Log.Information("GitVersion: SemVer={SemVer} Branch={Branch}", info.SemVer, info.BranchName);

        if (NukeBuild.IsServerBuild)
        {
            AssertStableOnlyFromReleaseBranch(info);
            AssertVersionAvailable(info, TagPrefix, GitTags.All(), GitTags.OnHead(TagPrefix));
        }

        return info;
    }

    /// <summary>
    /// Reads <c>gitversion /output json</c>. Public so the output contract is pinned by tests against real
    /// tool output rather than only by a build that has already pushed packages - GitVersion has changed
    /// which variables it emits between major versions, and a field that quietly stops arriving reads as an
    /// empty string here, not as an error.
    /// </summary>
    /// <exception cref="InvalidOperationException">GitVersion produced no output.</exception>
    public static GitVersionInfo Parse(string json)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new LenientStringConverter());

        return JsonSerializer.Deserialize<GitVersionInfo>(json, options)
               ?? throw new InvalidOperationException("GitVersion returned empty output");
    }

    /// <summary>
    /// A version without a pre-release label is a stable release, and only <c>main</c>/<c>master</c> may
    /// produce one. Everywhere else - develop, a detached HEAD, a branch GitVersion could not name - a
    /// label-less version means the version was mangled on the way to the packages, not that a release was
    /// intended. That is not recoverable after the push: the stable version outranks every pre-release that
    /// follows it, so the whole alpha line disappears behind it. The build stops here instead.
    /// </summary>
    /// <exception cref="InvalidOperationException">A stable version was resolved off a release branch.</exception>
    public static void AssertStableOnlyFromReleaseBranch(GitVersionInfo info)
    {
        if (!string.IsNullOrEmpty(info.PreReleaseTag)
            || IsStableReleaseBranch(info.BranchName))
            return;

        throw new InvalidOperationException(
            $"Version {info.SemVer} carries no pre-release label, but it was resolved on branch "
            + $"'{info.BranchName}' rather than main/master. Publishing it would take the stable slot on the "
            + "feed and outrank every pre-release built after it. Fix the version resolution, or release "
            + "from main/master.");
    }

    /// <summary>Branches a stable, label-less version may be released from.</summary>
    public static bool IsStableReleaseBranch(string? branch) => branch is "main" or "master";

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

    // No NuGetVersion/NuGetVersionV2/NuGetPreReleaseTag* here: GitVersion 6 dropped those variables.
    // Declared, they deserialize to empty and read as "this build has no pre-release label".

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
    // Without this the serializer handles a JSON null itself and writes it straight into a non-nullable
    // string property, past both the converter and the "" initialiser - GitVersion emits null for the
    // metadata it has nothing to put in, so every consumer of these fields would need a null check the
    // type says it does not need.
    public override bool HandleNull => true;

    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number
            ? reader.TryGetInt64(out var n)
                ? n.ToString()
                : reader.GetDouble().ToString(CultureInfo.InvariantCulture)
            : reader.GetString() ?? "";

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}