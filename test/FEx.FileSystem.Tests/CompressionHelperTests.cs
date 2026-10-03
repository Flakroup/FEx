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

    private string CreateZip(string entryName, string content)
    {
        var path = Path.Combine(_dir, $"{Guid.NewGuid()}.zip");

        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);

        return path;
    }
}
