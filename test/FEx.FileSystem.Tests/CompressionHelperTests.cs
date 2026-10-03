using FEx.FileSystem.Helpers;
using Shouldly;
using System;
using System.IO;
using System.IO.Compression;
using Xunit;

namespace FEx.FileSystem.Tests;

public sealed class CompressionHelperTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"FExZipTest_{Guid.NewGuid()}");

    public CompressionHelperTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not a test failure.
        }
    }

    // Pins #98 on non-Windows runners only: the old hardcoded backslash is a valid separator on Windows, so
    // there the code behaved the same and no test can tell the two apart. CI runs on Windows; this guards Linux/macOS.
    [Fact]
    public void DecompressZip_Overwrite_ReplacesNestedFilesThatAlreadyExist()
    {
        var zip = CreateZip("sub/dir/file.txt", "new");
        var target = Path.Combine(_dir, "out");
        var nested = Path.Combine(target, "sub", "dir", "file.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(nested)!);
        File.WriteAllText(nested, "old");

        CompressionHelper.DecompressZip(zip, target, true);

        File.ReadAllText(nested).ShouldBe("new");
    }

    [Fact]
    public void DecompressZip_EntryEscapingTheTarget_ThrowsAndLeavesTheOutsideFileAlone()
    {
        var zip = CreateZip("../outside.txt", "new");
        var target = Path.Combine(_dir, "out");
        var outside = Path.Combine(_dir, "outside.txt");
        File.WriteAllText(outside, "old");

        Should.Throw<InvalidDataException>(() => CompressionHelper.DecompressZip(zip, target, true));

        File.ReadAllText(outside).ShouldBe("old");
    }

    [Fact]
    public void DecompressZip_EscapingDirectoryEntry_ThrowsBeforeAnyExistingFileIsDeleted()
    {
        var zip = CreateZip(("../evil/", ""), ("a.txt", "new"));
        var target = Path.Combine(_dir, "out");
        var existing = Path.Combine(target, "a.txt");
        Directory.CreateDirectory(target);
        File.WriteAllText(existing, "old");

        Should.Throw<InvalidDataException>(() => CompressionHelper.DecompressZip(zip, target, true));

        File.ReadAllText(existing).ShouldBe("old");
    }

    // The path comparison follows the file system, like ZipFile.ExtractToDirectory: "../OUT" is the target itself
    // on Windows and a different directory elsewhere.
    [Fact]
    public void DecompressZip_DifferentlyCasedTargetEntry_IsAcceptedOnWindows()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Case-insensitive file systems only");

        var zip = CreateZip(("../OUT/x.txt", "new"));
        var target = Path.Combine(_dir, "out");

        CompressionHelper.DecompressZip(zip, target);

        File.ReadAllText(Path.Combine(target, "x.txt")).ShouldBe("new");
    }

    [Fact]
    public void DecompressZip_DifferentlyCasedTargetEntry_IsRejectedOffWindows()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Case-sensitive file systems only");

        var zip = CreateZip(("../OUT/x.txt", "new"));

        Should.Throw<InvalidDataException>(() => CompressionHelper.DecompressZip(zip, Path.Combine(_dir, "out")));
    }

    private string CreateZip(string entryName, string content) => CreateZip((entryName, content));

    private string CreateZip(params (string Name, string Content)[] entries)
    {
        var path = Path.Combine(_dir, $"{Guid.NewGuid()}.zip");

        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);

        foreach (var (name, content) in entries)
        {
            var entry = archive.CreateEntry(name);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }

        return path;
    }
}
