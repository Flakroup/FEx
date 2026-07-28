using Shouldly;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// A published commit must end up with exactly one version tag. The name-only check that used to guard
/// this let a second tag land whenever GitVersion produced a new SemVer for an already-tagged commit,
/// so the skip decision is pinned here case by case.
/// </summary>
public sealed class TagTargetTests
{
    [Fact]
    public void UntaggedHeadWithFreeName_IsTagged()
    {
        ITagTarget.DescribeTagSkip("v0.1.1-alpha.412", [], tagNameTaken: false).ShouldBeNull();
    }

    [Fact]
    public void SameTagAlreadyOnHead_IsSkipped()
    {
        // A plain re-run of the publish workflow on an unchanged commit.
        ITagTarget.DescribeTagSkip("v0.1.1-alpha.412", ["v0.1.1-alpha.412"], tagNameTaken: true)
            .ShouldBe("HEAD already carries it");
    }

    [Fact]
    public void DifferentVersionTagOnHead_IsSkipped()
    {
        // The bug: version base moved 0.1.0 -> 0.1.1, so the free name v0.1.1-alpha.412 sailed past the
        // name check and stacked a second tag onto the commit already tagged v0.1.0-alpha.412.
        ITagTarget.DescribeTagSkip("v0.1.1-alpha.412", ["v0.1.0-alpha.412"], tagNameTaken: false)
            .ShouldBe("HEAD already carries version tag(s) v0.1.0-alpha.412");
    }

    [Fact]
    public void SeveralVersionTagsOnHead_AreAllNamedInTheReason()
    {
        ITagTarget.DescribeTagSkip("v0.2.0-alpha.9", ["v0.1.0-alpha.9", "v0.1.1-alpha.9"], tagNameTaken: false)
            .ShouldBe("HEAD already carries version tag(s) v0.1.0-alpha.9, v0.1.1-alpha.9");
    }

    [Fact]
    public void NameTakenByAnotherCommit_IsSkippedRatherThanFailingTheBuild()
    {
        // `git tag` would exit non-zero here and take the whole publish down with it.
        ITagTarget.DescribeTagSkip("v0.1.1-alpha.412", [], tagNameTaken: true)
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
}
