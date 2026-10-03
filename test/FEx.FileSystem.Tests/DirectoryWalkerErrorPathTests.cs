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
    public void IsErrorPath_DescendantOfRecordedDirectory_IsRecognised()
    {
        var root = UniqueRoot();
        new DirectoryInfo(Path.Combine(root, "Failed")).AddToErrorPaths();

        new DirectoryInfo(Path.Combine(root, "Failed", "Child")).IsErrorPath().ShouldBeTrue();
    }

    [Fact]
    public void IsErrorPath_DirectoryDifferingOnlyByCase_IsNotRecognised()
    {
        // ErrorPaths is case-sensitive, so the descendant check must not fold case either.
        var root = UniqueRoot();
        new DirectoryInfo(Path.Combine(root, "Failed")).AddToErrorPaths();

        new DirectoryInfo(Path.Combine(root, "failed")).IsErrorPath().ShouldBeFalse();
        new DirectoryInfo(Path.Combine(root, "failed", "Child")).IsErrorPath().ShouldBeFalse();
    }

    [Fact]
    public void IsErrorPath_IgnorableCharacterInTheName_IsNotTreatedAsTheRecordedDirectory()
    {
        // A culture-sensitive StartsWith skips zero-width characters such as the soft hyphen; the ordinal rule
        // that Contains uses does not.
        var root = UniqueRoot();
        new DirectoryInfo(Path.Combine(root, "Fail\u00ADed")).AddToErrorPaths();

        new DirectoryInfo(Path.Combine(root, "Failed", "Child")).IsErrorPath().ShouldBeFalse();
    }

    [Fact]
    public void IsErrorPath_SiblingSharingTheNamePrefix_IsNotRecognised()
    {
        var root = UniqueRoot();
        new DirectoryInfo(Path.Combine(root, "Failed")).AddToErrorPaths();

        new DirectoryInfo(Path.Combine(root, "FailedToo")).IsErrorPath().ShouldBeFalse();
    }
}
