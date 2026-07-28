using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// Publishing is idempotent per commit: the version tag left by the first run marks the commit as
/// released, and every later run must be a no-op. These pin the two halves of that - the version
/// resolver refusing to invent a new version for an already-tagged commit, and the tag step refusing
/// to stack a second tag on it.
/// </summary>
public sealed class TagTargetTests
{
    [Fact]
    public void NoCollidingTag_KeepsTheResolvedVersion()
    {
        var bumped = IGitVersionComponent.BumpPastTags(Version(0, 1, 0), Tags(), Tags());

        bumped.SemVer.ShouldBe("0.1.0-alpha.412");
    }

    [Fact]
    public void TagOnHead_KeepsTheVersionInsteadOfBumping()
    {
        // The bug: a second publish of the same commit saw its own tag from the first run, bumped the
        // patch to a version nobody asked for, and shipped identical sources again as 0.1.1.
        var bumped = IGitVersionComponent.BumpPastTags(
            Version(0, 1, 0),
            Tags("v0.1.0-alpha.412"),
            Tags("v0.1.0-alpha.412"));

        bumped.SemVer.ShouldBe("0.1.0-alpha.412");
    }

    [Fact]
    public void TagOnAnotherCommit_StillBumpsPastTheNameCollision()
    {
        var bumped = IGitVersionComponent.BumpPastTags(Version(0, 1, 0), Tags("v0.1.0-alpha.412"), Tags());

        bumped.SemVer.ShouldBe("0.1.1-alpha.412");
        bumped.Patch.ShouldBe(1);
    }

    [Fact]
    public void ChainOfTakenNames_BumpsPastAllOfThem()
    {
        var bumped = IGitVersionComponent.BumpPastTags(
            Version(0, 1, 0),
            Tags("v0.1.0-alpha.412", "v0.1.1-alpha.412", "v0.1.2-alpha.412"),
            Tags());

        bumped.SemVer.ShouldBe("0.1.3-alpha.412");
    }

    [Fact]
    public void HeadTagReachedWhileBumping_StopsTheChain()
    {
        // v0.1.0 collided with another commit, but v0.1.1 is HEAD's own published version - stop there
        // rather than bumping past a release this very commit already made.
        var bumped = IGitVersionComponent.BumpPastTags(
            Version(0, 1, 0),
            Tags("v0.1.0-alpha.412", "v0.1.1-alpha.412"),
            Tags("v0.1.1-alpha.412"));

        bumped.SemVer.ShouldBe("0.1.1-alpha.412");
    }

    [Fact]
    public void UntaggedHeadWithFreeName_IsTagged()
    {
        ITagTarget.DescribeTagSkip("v0.1.1-alpha.412", Tags(), tagNameTaken: false).ShouldBeNull();
    }

    [Fact]
    public void SameTagAlreadyOnHead_IsSkipped()
    {
        // A plain re-run of the publish workflow on an unchanged commit.
        ITagTarget.DescribeTagSkip("v0.1.1-alpha.412", Tags("v0.1.1-alpha.412"), tagNameTaken: true)
            .ShouldBe("HEAD already carries it");
    }

    [Fact]
    public void DifferentVersionTagOnHead_IsSkipped()
    {
        ITagTarget.DescribeTagSkip("v0.1.1-alpha.412", Tags("v0.1.0-alpha.412"), tagNameTaken: false)
            .ShouldBe("HEAD already carries version tag(s) v0.1.0-alpha.412");
    }

    [Fact]
    public void SeveralVersionTagsOnHead_AreAllNamedInTheReason()
    {
        // Ordered, so the message does not shuffle between runs on the underlying set.
        ITagTarget.DescribeTagSkip("v0.2.0-alpha.9", Tags("v0.1.1-alpha.9", "v0.1.0-alpha.9"), tagNameTaken: false)
            .ShouldBe("HEAD already carries version tag(s) v0.1.0-alpha.9, v0.1.1-alpha.9");
    }

    [Fact]
    public void NameTakenByAnotherCommit_IsSkippedRatherThanFailingTheBuild()
    {
        // `git tag` would exit non-zero here and take the whole publish down with it.
        ITagTarget.DescribeTagSkip("v0.1.1-alpha.412", Tags(), tagNameTaken: true)
            .ShouldBe("the name is taken by another commit");
    }

    [Theory]
    [InlineData("main")]
    [InlineData("master")]
    [InlineData("develop")]
    public void ReleaseBranches_AreTagged(string branch)
    {
        ITagTarget.IsReleaseBranch(branch).ShouldBeTrue();
    }

    [Theory]
    [InlineData("feature/pack-fex-building")]
    [InlineData("release/1.0")]
    [InlineData("Main")]
    [InlineData(null)]
    public void OtherBranches_AreNotTagged(string? branch)
    {
        ITagTarget.IsReleaseBranch(branch).ShouldBeFalse();
    }

    private static ISet<string> Tags(params string[] tags) => new HashSet<string>(tags);

    private static GitVersionInfo Version(int major, int minor, int patch) =>
        new()
        {
            Major = major,
            Minor = minor,
            Patch = patch,
            MajorMinorPatch = $"{major}.{minor}.{patch}",
            PreReleaseTagWithDash = "-alpha.412",
            NuGetPreReleaseTagV2 = "alpha.412",
            SemVer = $"{major}.{minor}.{patch}-alpha.412",
            BranchName = "develop",
            Sha = "ffe637fa5fbea9d7f9293329a4a7c388d3d648fa"
        };
}
