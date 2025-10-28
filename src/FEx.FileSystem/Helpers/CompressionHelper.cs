using FEx.Agnostics.Abstractions.Extensions;
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

        if (overwrite)
            //todo check if directories entries are also important
            foreach (var entry in ListZipEntries(fileToDecompress)
                         .Where(x => !x.FullName.EndsWith("/"))
                         .Select(x => Path.Combine(targetDirectory, x.FullName.Replace("/", "\\")))
                         .Where(File.Exists)
                         .ToArray())
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