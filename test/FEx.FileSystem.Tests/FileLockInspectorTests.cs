using Shouldly;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace FEx.FileSystem.Tests;

public sealed class FileLockInspectorTests
{
    [Fact]
    public void WhoIsLocking_NullPath_ReturnsEmpty()
    {
        // Act
        var result = FileLockInspector.WhoIsLocking((string?)null);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void WhoIsLocking_UnlockedFile_ReturnsEmpty()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
        File.WriteAllText(path, "data");

        try
        {
            // Act
            var result = FileLockInspector.WhoIsLocking(path);

            // Assert
            result.ShouldBeEmpty();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void WhoIsLocking_LockedFile_ReportsCurrentProcess()
    {
        Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Restart Manager is only available on Windows");

        // Arrange
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");

        try
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            stream.WriteByte(1);
            stream.Flush();

            // Act
            var result = FileLockInspector.WhoIsLocking(path);

            // Assert
            result.ShouldNotBeEmpty();
            result.ShouldContain(info => info.ProcessId == Environment.ProcessId);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
