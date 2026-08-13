using Nuke.Common.IO;
using Shouldly;
using System;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The publish target writes one application per output directory - by default one derived from the project
/// file name, otherwise one the consuming build names itself. That is a silent-failure shape: two entries
/// sharing a directory publish over each other, with a green build shipping one application where two were
/// expected.
/// </summary>
public sealed class AppPublishLayoutTests
{
    [Theory]
    [InlineData(@"X:\repo\src\Sample.Api\Sample.Api.csproj", "Sample.Api")]
    [InlineData("src/Sample.Api/Sample.Api.csproj", "Sample.Api")]
    [InlineData("Sample.Api.csproj", "Sample.Api")]
    public void OutputName_IsTheProjectFileNameWithoutItsExtension(string project, string expected)
    {
        AppPublishLayout.OutputName(project).ShouldBe(expected);
    }

    [Fact]
    public void OutputName_KeepsEveryDottedSegmentOfTheProjectName()
    {
        // A .NET project name is dotted far more often than not, and only the EXTENSION may be dropped.
        // Given a bare name rather than a path, the same call would answer "Sample" - which is why
        // PublishEntries has to hand this a project file path.
        AppPublishLayout.OutputName("src/Sample.Api.Host/Sample.Api.Host.csproj").ShouldBe("Sample.Api.Host");
    }

    [Fact]
    public void DistinctOutputDirectories_DoNotCollide()
    {
        Should.NotThrow(static () => AppPublishLayout.EnsureNoOutputCollision([
            Entry("/repo/src/Sample.Api/Sample.Api.csproj", "/artifacts/publish/Sample.Api"),
            Entry("/repo/src/Sample.Seeder/Sample.Seeder.csproj", "/artifacts/publish/Sample.Seeder")
        ]));
    }

    [Fact]
    public void SameFileNameInDifferentDirectories_FailsInsteadOfOverwriting()
    {
        // What the default layout produces for two projects that share a file name.
        var error = Should.Throw<InvalidOperationException>(static () => AppPublishLayout.EnsureNoOutputCollision([
            Entry("/repo/apps/web/Host.csproj", "/artifacts/publish/Host"),
            Entry("/repo/apps/admin/Host.csproj", "/artifacts/publish/Host")
        ]));

        error.Message.ShouldContain("web");
        error.Message.ShouldContain("admin");
    }

    [Fact]
    public void DistinctProjectsPointedAtOneDirectory_Fail()
    {
        // Only reachable through an explicit PublishEntries override - the names differ, so a check on the
        // project name alone would wave this through and one application would overwrite the other.
        Should.Throw<InvalidOperationException>(static () => AppPublishLayout.EnsureNoOutputCollision([
            Entry("/repo/src/Sample.Api/Sample.Api.csproj", "/publish/app"),
            Entry("/repo/src/Sample.Worker/Sample.Worker.csproj", "/publish/app")
        ]));
    }

    [Fact]
    public void CollisionCheck_IsCaseInsensitive()
    {
        // Windows and Linux disagree about whether these are one directory; the build must not.
        Should.Throw<InvalidOperationException>(static () => AppPublishLayout.EnsureNoOutputCollision([
            Entry("/repo/a/Host.csproj", "/publish/host"),
            Entry("/repo/b/HOST.csproj", "/publish/HOST")
        ]));
    }

    [Fact]
    public void NoEntries_IsNotACollision()
    {
        Should.NotThrow(static () => AppPublishLayout.EnsureNoOutputCollision([]));
    }

    // AppPublishEntry.ProjectPath is an AbsolutePath, which rejects a relative path outright - so the
    // fixtures are rooted even where the test only cares about the file name.
    private static AppPublishEntry Entry(string project, string output) =>
        new((AbsolutePath)project, (AbsolutePath)output);
}
