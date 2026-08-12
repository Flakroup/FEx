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
    [InlineData(@"X:\repo\src\Trippy.Api\Trippy.Api.csproj", "Trippy.Api")]
    [InlineData("src/Trippy.Api/Trippy.Api.csproj", "Trippy.Api")]
    [InlineData("Trippy.Api.csproj", "Trippy.Api")]
    public void OutputName_IsTheProjectFileNameWithoutItsExtension(string project, string expected)
    {
        AppPublishLayout.OutputName(project).ShouldBe(expected);
    }

    [Fact]
    public void DistinctOutputDirectories_DoNotCollide()
    {
        Should.NotThrow(static () => AppPublishLayout.EnsureNoOutputCollision([
            Entry("src/Trippy.Api/Trippy.Api.csproj", "/artifacts/publish/Trippy.Api"),
            Entry("src/Trippy.Seeder/Trippy.Seeder.csproj", "/artifacts/publish/Trippy.Seeder")
        ]));
    }

    [Fact]
    public void SameFileNameInDifferentDirectories_FailsInsteadOfOverwriting()
    {
        // What the default layout produces for two projects that share a file name.
        var error = Should.Throw<InvalidOperationException>(static () => AppPublishLayout.EnsureNoOutputCollision([
            Entry("apps/web/Host.csproj", "/artifacts/publish/Host"),
            Entry("apps/admin/Host.csproj", "/artifacts/publish/Host")
        ]));

        error.Message.ShouldContain("apps/web/Host.csproj");
        error.Message.ShouldContain("apps/admin/Host.csproj");
    }

    [Fact]
    public void DistinctProjectsPointedAtOneDirectory_Fail()
    {
        // Only reachable through an explicit PublishEntries override - the names differ, so a check on the
        // project name alone would wave this through and one application would overwrite the other.
        Should.Throw<InvalidOperationException>(static () => AppPublishLayout.EnsureNoOutputCollision([
            Entry("src/Bootstrapper/Bootstrapper.csproj", "/publish/api"),
            Entry("src/HalEmulator/HalEmulator.csproj", "/publish/api")
        ]));
    }

    [Fact]
    public void CollisionCheck_IsCaseInsensitive()
    {
        // Windows and Linux disagree about whether these are one directory; the build must not.
        Should.Throw<InvalidOperationException>(static () => AppPublishLayout.EnsureNoOutputCollision([
            Entry("a/Host.csproj", "/publish/host"),
            Entry("b/HOST.csproj", "/publish/HOST")
        ]));
    }

    [Fact]
    public void NoEntries_IsNotACollision()
    {
        Should.NotThrow(static () => AppPublishLayout.EnsureNoOutputCollision([]));
    }

    private static AppPublishEntry Entry(string project, string output) =>
        new(project, (AbsolutePath)output);
}
