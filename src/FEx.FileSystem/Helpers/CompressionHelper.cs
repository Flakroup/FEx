using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.FileSystem.Helpers;

public static class CompressionHelper
{
    public static async Task<FileInfo> DecompressGZipAsync(this FileInfo fileToDecompress,
                                                           string targetDirectory,
                                                           string targetFileName)
    {
        var dir = new DirectoryInfo(targetDirectory);
        dir.Create();
        var file = dir.GetDescendantFile(targetFileName);

        using (var originalFileStream = fileToDecompress.OpenRead())
        using (var decompressedFileStream = file.Create())
        using (var decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
            await decompressionStream.CopyToAsync(decompressedFileStream);

        file.Refresh();

        return file;
    }

    public static bool VerifyGZip(this FileInfo fileToDecompress)
    {
        try
        {
            using var originalFileStream = fileToDecompress.OpenRead();

            using (new GZipStream(originalFileStream, CompressionMode.Decompress))
                return true;
        }
        catch
        {
            //ignored
        }

        return false;
    }

    public static void DecompressZip(string fileToDecompress, string targetDirectory, bool overwrite = false)
    {
        Directory.CreateDirectory(targetDirectory);

        var root = Path.GetFullPath(targetDirectory);

        if (!root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            root += Path.DirectorySeparatorChar;

        // Resolved before anything is deleted: an entry such as "../x" must not reach outside the target, and the
        // overwrite pre-delete below would otherwise remove a file there.
        var destinations = ListZipEntries(fileToDecompress)
            .Where(x => !x.FullName.EndsWith("/", StringComparison.Ordinal))
            .Select(x => Path.GetFullPath(Path.Combine(root, x.FullName)))
            .ToArray();

        foreach (var destination in destinations)
            if (!destination.StartsWith(root, StringComparison.Ordinal))
                throw new InvalidDataException($"Zip entry resolves outside the target directory: {destination}");

        if (overwrite)
            //todo check if directories entries are also important
            foreach (var entry in destinations.Where(File.Exists))
            {
                var isSuccess = false;

                while (!isSuccess)
                    isSuccess = FileSystemUtilities.SafeDeleteFile(entry).IsSuccess;
            }

        ZipFile.ExtractToDirectory(fileToDecompress, targetDirectory);
    }

    public static ZipArchiveEntry[] ListZipEntries(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);

        return [.. archive.Entries];
    }
}