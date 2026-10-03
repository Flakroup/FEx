using Shouldly;
using System;
using System.IO;
using Xunit;

namespace FEx.FileSystem.Tests;

public sealed class DirectoryWalkerErrorPathTests
{
    private static string UniqueRoot() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public void IsErrorPath_RecordedDirectory_IsRecognised()
    {
        var failed = new DirectoryInfo(Path.Combine(UniqueRoot(), "Failed"));
        failed.AddToErrorPaths();

        failed.IsErrorPath().ShouldBeTrue();
    }

    [Fact]
    public void IsErrorPath_RecordedDirectoryReachedWithDifferentCasing_IsRecognised()
    {
        var root = UniqueRoot();
        new DirectoryInfo(Path.Combine(root, "Failed")).AddToErrorPaths();

        new DirectoryInfo(Path.Combine(root, "FAILED")).IsErrorPath().ShouldBeTrue();
    }

    [Fact]
    public void IsErrorPath_DescendantOfRecordedDirectoryWithDifferentCasing_IsRecognised()
    {
        var root = UniqueRoot();
        new DirectoryInfo(Path.Combine(root, "Failed")).AddToErrorPaths();

        new DirectoryInfo(Path.Combine(root, "failed", "Child")).IsErrorPath().ShouldBeTrue();
    }

    [Fact]
    public void IsErrorPath_SiblingSharingTheNamePrefix_IsNotRecognised()
    {
        var root = UniqueRoot();
        new DirectoryInfo(Path.Combine(root, "Failed")).AddToErrorPaths();

        new DirectoryInfo(Path.Combine(root, "FailedToo")).IsErrorPath().ShouldBeFalse();
    }
}
