using Shouldly;
using System;
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
    public void NoCollidingTag_IsAvailable()
    {
        Should.NotThrow(() => IGitVersionComponent.AssertVersionAvailable(Version(), "v", Tags(), Tags()));
    }

    [Fact]
    public void TagOnHead_IsAvailable_BecauseTheCommitKeepsItsReleasedVersion()
    {
        // The bug: a second publish of the same commit saw its own tag from the first run and invented
        // 0.1.1 to get around it, shipping identical sources under a version nobody planned.
        Should.NotThrow(() => IGitVersionComponent.AssertVersionAvailable(
            Version(),
            "v",
            Tags("v0.1.0-alpha.412"),
            Tags("v0.1.0-alpha.412")));
    }

    [Fact]
    public void TagOnAnotherCommit_StopsTheBuildInsteadOfInventingAVersion()
    {
        var error = Should.Throw<InvalidOperationException>(() => IGitVersionComponent.AssertVersionAvailable(
            Version(),
            "v",
            Tags("v0.1.0-alpha.412"),
            Tags()));

        error.Message.ShouldContain("v0.1.0-alpha.412");
    }

    [Fact]
    public void NonDefaultPrefix_IsRecognisedOnHeadRatherThanTreatedAsACollision()
    {
        // The prefix reaches the resolver, the publish gate and the tag step from one parameter. Read from
        // two places, a custom prefix would leave this looking like a foreign tag and fail the build.
        Should.NotThrow(() => IGitVersionComponent.AssertVersionAvailable(
            Version(),
            "rel-",
            Tags("rel-0.1.0-alpha.412"),
            Tags("rel-0.1.0-alpha.412")));
    }

    [Fact]
    public void UntaggedHead_IsNotAReleasedCommit()
    {
        GitTags.MarksReleasedCommit(Tags()).ShouldBeFalse();
    }

    [Fact]
    public void HeadCarryingAVersionTag_IsAReleasedCommit()
    {
        // What stops the publish gate from pushing packages a second time.
        GitTags.MarksReleasedCommit(Tags("v0.1.0-alpha.412")).ShouldBeTrue();
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

    // Shaped like the develop pre-release GitVersion actually emits here (ContinuousDeployment mode).
    private static GitVersionInfo Version() =>
        new()
        {
            Major = 0,
            Minor = 1,
            Patch = 0,
            MajorMinorPatch = "0.1.0",
            PreReleaseTag = "alpha.412",
            PreReleaseTagWithDash = "-alpha.412",
            SemVer = "0.1.0-alpha.412",
            BranchName = "develop",
            Sha = "ffe637fa5fbea9d7f9293329a4a7c388d3d648fa"
        };
}
