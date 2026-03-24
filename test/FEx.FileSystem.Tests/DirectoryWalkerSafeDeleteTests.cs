using FEx.FileSystem;
using Shouldly;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace FEx.FileSystem.Tests;

public sealed class DirectoryWalkerSafeDeleteTests
{
    // ── DirectoryInfo overload ──────────────────────────────────────────────

    [Fact]
    public void SafeDelete_NonExistentDirectory_ReturnsTrue()
    {
        // Arrange
        var dir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        // Act
        var result = dir.SafeDelete(recursive: false);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void SafeDelete_EmptyDirectory_NonRecursive_DeletesAndReturnsTrue()
    {
        // Arrange
        var dir = Directory.CreateDirectory(TempPath());

        try
        {
            // Act
            var result = new DirectoryInfo(dir.FullName).SafeDelete(recursive: false);

            // Assert
            result.ShouldBeTrue();
            dir.Refresh();
            dir.Exists.ShouldBeFalse();
        }
        finally
        {
            if (dir.Exists) dir.Delete(true);
        }
    }

    [Fact]
    public void SafeDelete_DirectoryWithFiles_NonRecursive_ReturnsFalse()
    {
        // Arrange
        var dir = Directory.CreateDirectory(TempPath());

        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "file.txt"), "data");

            // Act
            var result = new DirectoryInfo(dir.FullName).SafeDelete(recursive: false);

            // Assert
            result.ShouldBeFalse();
            dir.Refresh();
            dir.Exists.ShouldBeTrue();
        }
        finally
        {
            if (dir.Exists) dir.Delete(true);
        }
    }

    [Fact]
    public void SafeDelete_DirectoryWithFiles_Recursive_DeletesAllAndReturnsTrue()
    {
        // Arrange
        var dir = Directory.CreateDirectory(TempPath());

        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "a.txt"), "data");
            File.WriteAllText(Path.Combine(dir.FullName, "b.txt"), "data");

            // Act
            var result = new DirectoryInfo(dir.FullName).SafeDelete(recursive: true);

            // Assert
            result.ShouldBeTrue();
            dir.Refresh();
            dir.Exists.ShouldBeFalse();
        }
        finally
        {
            if (dir.Exists) dir.Delete(true);
        }
    }

    [Fact]
    public void SafeDelete_DirectoryWithNestedStructure_Recursive_DeletesAllAndReturnsTrue()
    {
        // Arrange
        var dir = Directory.CreateDirectory(TempPath());

        try
        {
            var sub = dir.CreateSubdirectory("sub");
            var deep = sub.CreateSubdirectory("deep");
            File.WriteAllText(Path.Combine(dir.FullName, "root.txt"), "data");
            File.WriteAllText(Path.Combine(sub.FullName, "sub.txt"), "data");
            File.WriteAllText(Path.Combine(deep.FullName, "deep.txt"), "data");

            // Act
            var result = new DirectoryInfo(dir.FullName).SafeDelete(recursive: true);

            // Assert
            result.ShouldBeTrue();
            dir.Refresh();
            dir.Exists.ShouldBeFalse();
        }
        finally
        {
            if (dir.Exists) dir.Delete(true);
        }
    }

    [Fact]
    public void SafeDelete_DirectoryWithLockedFile_Recursive_ReturnsFalseAndLeavesLockedFile()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        // Arrange
        var dir = Directory.CreateDirectory(TempPath());

        try
        {
            var lockedPath = Path.Combine(dir.FullName, "locked.dll");
            var freePath = Path.Combine(dir.FullName, "free.txt");
            File.WriteAllText(lockedPath, "locked");
            File.WriteAllText(freePath, "free");

            using var lockStream = new FileStream(lockedPath, FileMode.Open, FileAccess.Read, FileShare.None);

            // Act
            var result = new DirectoryInfo(dir.FullName).SafeDelete(recursive: true);

            // Assert
            result.ShouldBeFalse();
            File.Exists(lockedPath).ShouldBeTrue();
            File.Exists(freePath).ShouldBeFalse();
        }
        finally
        {
            if (dir.Exists) dir.Delete(true);
        }
    }

    // ── FileInfo overload ───────────────────────────────────────────────────

    [Fact]
    public void SafeDelete_NonExistentFile_ReturnsTrue()
    {
        // Arrange
        var file = new FileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp"));

        // Act
        var result = file.SafeDelete();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void SafeDelete_ExistingFile_DeletesAndReturnsTrue()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
        File.WriteAllText(path, "data");

        try
        {
            // Act
            var result = new FileInfo(path).SafeDelete();

            // Assert
            result.ShouldBeTrue();
            File.Exists(path).ShouldBeFalse();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void SafeDelete_LockedFile_ReturnsFalse()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        // Arrange
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
        File.WriteAllText(path, "data");

        try
        {
            using var lockStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);

            // Act
            var result = new FileInfo(path).SafeDelete();

            // Assert
            result.ShouldBeFalse();
            File.Exists(path).ShouldBeTrue();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static string TempPath() =>
        Path.Combine(Path.GetTempPath(), $"FExTest_{Guid.NewGuid()}");
}
