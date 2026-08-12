using Shouldly;
using System;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The packages carry whatever version this component resolves, and a version can only be published once.
/// These pin the two ways that went wrong: the GitVersion output contract the record reads, and the rule
/// that a label-less stable version may only come off a release branch.
/// </summary>
public sealed class GitVersionComponentTests
{
    // Verbatim `gitversion /output json` from GitVersion 6.3.0 on this repository's develop branch. The
    // NuGetVersion*/NuGetPreReleaseTag* variables that GitVersion 5 emitted are gone - reading them left
    // the pre-release label empty and shipped a develop snapshot as a stable release.
    private const string GitVersion63Json = """
        {
          "AssemblySemFileVer": "0.1.1.0",
          "AssemblySemVer": "0.1.1.0",
          "BranchName": "develop",
          "BuildMetaData": null,
          "CommitDate": "2026-08-03",
          "CommitsSinceVersionSource": 0,
          "EscapedBranchName": "develop",
          "FullBuildMetaData": "Branch.develop.Sha.035d3400cc81a5306d3e792244fd75c9435de791",
          "FullSemVer": "0.1.1-alpha.437",
          "InformationalVersion": "0.1.1-alpha.437+Branch.develop.Sha.035d3400cc81a5306d3e792244fd75c9435de791",
          "Major": 0,
          "MajorMinorPatch": "0.1.1",
          "Minor": 1,
          "Patch": 1,
          "PreReleaseLabel": "alpha",
          "PreReleaseLabelWithDash": "-alpha",
          "PreReleaseNumber": 437,
          "PreReleaseTag": "alpha.437",
          "PreReleaseTagWithDash": "-alpha.437",
          "SemVer": "0.1.1-alpha.437",
          "Sha": "035d3400cc81a5306d3e792244fd75c9435de791",
          "ShortSha": "035d340",
          "UncommittedChanges": 1,
          "VersionSourceSha": "f9a86390565f8ac9d54926ab535ac494c240887c",
          "WeightedPreReleaseNumber": 437
        }
        """;

    [Fact]
    public void GitVersion6Output_KeepsThePreReleaseLabelOnEveryFieldThePackagesUse()
    {
        var info = IGitVersionComponent.Parse(GitVersion63Json);

        info.SemVer.ShouldBe("0.1.1-alpha.437");
        info.PreReleaseTag.ShouldBe("alpha.437");
        info.InformationalVersion.ShouldStartWith("0.1.1-alpha.437+");
        info.AssemblySemVer.ShouldBe("0.1.1.0");
        info.BranchName.ShouldBe("develop");
    }

    [Fact]
    public void NullBuildMetaData_DoesNotFailTheWholeResolution()
    {
        // GitVersion emits null, not "", for metadata it has nothing to put in.
        IGitVersionComponent.Parse(GitVersion63Json).BuildMetaData.ShouldBe("");
    }

    [Fact]
    public void PreReleaseNumber_ArrivesAsANumberAndIsReadAsOne()
    {
        IGitVersionComponent.Parse(GitVersion63Json).PreReleaseNumber.ShouldBe(437);
    }

    [Fact]
    public void EmptyToolOutput_StopsTheBuildRatherThanVersioningAt0_0_0()
    {
        Should.Throw<InvalidOperationException>(static () => IGitVersionComponent.Parse("null"));
    }

    [Fact]
    public void PreReleaseVersionOffAReleaseBranch_IsAllowed()
    {
        Should.NotThrow(static () => IGitVersionComponent.AssertStableOnlyFromReleaseBranch(Version("0.2.0-alpha.5",
            "alpha.5",
            "develop")));
    }

    [Theory]
    [InlineData("main")]
    [InlineData("master")]
    public void StableVersionFromAReleaseBranch_IsAllowed(string branch)
    {
        Should.NotThrow(() => IGitVersionComponent.AssertStableOnlyFromReleaseBranch(
            Version("1.0.0", preReleaseTag: "", branch)));
    }

    [Fact]
    public void StableVersionFromDevelop_StopsTheBuild()
    {
        // Exactly what shipped 0.1.1: develop's alpha resolved, then lost its label before packing.
        var error = Should.Throw<InvalidOperationException>(static () =>
            IGitVersionComponent.AssertStableOnlyFromReleaseBranch(Version("0.1.1", preReleaseTag: "", "develop")));

        error.Message.ShouldContain("0.1.1");
        error.Message.ShouldContain("develop");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Main")]
    [InlineData("feature/pack-fex-building")]
    public void StableVersionFromAnythingElse_StopsTheBuild(string branch)
    {
        // An unnamed branch (detached HEAD) is not a release branch either - fail closed, since the push
        // cannot be taken back.
        Should.Throw<InvalidOperationException>(() =>
            IGitVersionComponent.AssertStableOnlyFromReleaseBranch(Version("1.0.0", preReleaseTag: "", branch)));
    }

    private static GitVersionInfo Version(string semVer, string preReleaseTag, string branch) =>
        new() { SemVer = semVer, PreReleaseTag = preReleaseTag, BranchName = branch };
}
