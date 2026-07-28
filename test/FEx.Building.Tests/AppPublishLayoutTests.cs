using Shouldly;
using System;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The publish target derives each application's output folder from the project file name. That is a
/// silent-failure shape: two projects sharing a file name publish into one folder, the second overwriting
/// the first, with a green build shipping one application where two were expected.
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
    public void DistinctProjectNames_DoNotCollide()
    {
        Should.NotThrow(() => AppPublishLayout.EnsureNoOutputCollision(
            ["src/Trippy.Api/Trippy.Api.csproj", "src/Trippy.Seeder/Trippy.Seeder.csproj"]));
    }

    [Fact]
    public void SameFileNameInDifferentDirectories_FailsInsteadOfOverwriting()
    {
        var error = Should.Throw<InvalidOperationException>(() => AppPublishLayout.EnsureNoOutputCollision(
            ["apps/web/Host.csproj", "apps/admin/Host.csproj"]));

        error.Message.ShouldContain("Host");
        error.Message.ShouldContain("apps/web/Host.csproj");
        error.Message.ShouldContain("apps/admin/Host.csproj");
    }

    [Fact]
    public void CollisionCheck_IsCaseInsensitive()
    {
        // Windows and Linux disagree about whether these are one folder; the build must not.
        Should.Throw<InvalidOperationException>(() => AppPublishLayout.EnsureNoOutputCollision(
            ["a/Host.csproj", "b/HOST.csproj"]));
    }

    [Fact]
    public void NoProjects_IsNotACollision()
    {
        Should.NotThrow(() => AppPublishLayout.EnsureNoOutputCollision([]));
    }
}
