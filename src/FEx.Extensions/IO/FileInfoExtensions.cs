using FEx.Extensions.Helpers;
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FEx.Extensions.IO;

public static class FileInfoExtensions
{
    private static readonly int DefBufferSize = Convert.ToInt32(FileLengthConverter.ConvertFileLength(128, LengthType.Kilobytes, LengthType.Bytes, 0));

    /// <summary>
    ///     Compares the size.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="otherFileSize">Size of the other file.</param>
    /// <returns>
    ///     <para>Less than zero - This instance is smaller than the other.</para>
    ///     <para>Zero - This instance is equal by size to the other.</para>
    ///     <para>Greater than zero - This instance is bigger than the other.</para>
    /// </returns>
    public static int CompareSize(this FileInfo file, long otherFileSize)
    {
        return file.Length < otherFileSize ? -1 : file.Length > otherFileSize ? 1 : 0;
    }

    /// <summary>
    ///     Creates ZIP archive from file.
    /// </summary>
    /// <param name="file">The source file.</param>
    /// <param name="zipFilePath">
    ///     The ZIP file path. If null - ZIP file will be created next to source file with the same name
    ///     as original file.
    /// </param>
    /// <param name="deleteTempDirectory">if set to <c>true</c> [delete temporary directory].</param>
    /// <param name="overwrite">if set to <c>true</c> [overwrite].</param>
    /// <returns></returns>
    public static async Task<FileInfo> ZipAsync(this FileInfo file, string zipFilePath = null, bool deleteTempDirectory = false, bool overwrite = false)
    {
        return await file.ZipAsync(zipFilePath is not null
            ? new FileInfo(zipFilePath)
            : null, deleteTempDirectory, overwrite);
    }

    public static async Task<FileInfo> ZipAsync(this FileInfo file, FileInfo zipFile = null, bool deleteTempDirectory = false, bool overwrite = false)
    {
        DirectoryInfo parentDirectory = zipFile is null
            ? file.Directory
            : zipFile.Directory;
        var tempDirectory = new DirectoryInfo(Path.Combine(parentDirectory?.FullName, Path.GetFileNameWithoutExtension(file.Name)));

        if (tempDirectory.Exists && deleteTempDirectory)
            tempDirectory.Delete(true);

        tempDirectory.Create();
        string targetFilePath = Path.Combine(tempDirectory.FullName, file.Name);

#if NETSTANDARD
        using (FileStream sourceStream = file.OpenRead())
        using (FileStream targetStream =
 File.Open(targetFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
#else
        await using (FileStream sourceStream = file.OpenRead())
        await using (FileStream targetStream = File.Open(targetFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
#endif

            await sourceStream.CopyToAsync(targetStream);

        if (zipFile is null)
            zipFile = new(Path.Combine(parentDirectory?.FullName, $"{file.Name}.zip"));

        if (zipFile.Exists && overwrite)
        {
            zipFile.Delete();
            zipFile.Refresh();
        }

        ZipFile.CreateFromDirectory(tempDirectory.FullName, zipFile.FullName, CompressionLevel.Optimal, false);
        tempDirectory.Delete(true);
        zipFile.Refresh();
        return zipFile;
    }

    public static string GenerateMd5OfFile(this FileInfo file, bool removeDashes = true, bool toLower = true, bool asBase64String = false)
    {
        file.Refresh();

        byte[] hash = null;

        if (file.Exists)
            using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, DefBufferSize))
            using (var md5 = MD5.Create())
                hash = md5.ComputeHash(stream);

        return hash.GetHashString(removeDashes, toLower, asBase64String);
    }

    public static bool IsNtfs(this FileInfo file)
    {
        return FileSystemCommon.IsPathNtfs(file.FullName);
    }

    /// <summary>
    ///     Computes the md5 hash.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="removeDashes">if set to <c>true</c> [remove dashes].</param>
    /// <param name="toLower">if set to <c>true</c> [to lower].</param>
    /// <param name="asBase64String">if set to <c>true</c> [as base64 string].</param>
    public static string ComputeMd5Hash(this byte[] data, bool removeDashes = true, bool toLower = true, bool asBase64String = false)
    {
        byte[] hash;

        using (var md5Algorithm = MD5.Create())
            hash = md5Algorithm.ComputeHash(data);

        return hash.GetHashString(removeDashes, toLower, asBase64String);
    }

    public static async Task<MemoryStream> ToMemoryStreamAsync(this FileInfo file)
    {
        file.Refresh();

        if (!file.Exists)
            return null;

        return await new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, DefBufferSize).ToMemoryStreamAsync();
    }
}