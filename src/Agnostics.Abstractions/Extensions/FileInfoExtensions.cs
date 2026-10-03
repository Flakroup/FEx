using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Helpers;
using FEx.Agnostics.Abstractions.Utilities;
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for zipping, hashing and buffering files.</summary>
public static class FileInfoExtensions
{
    private static readonly int _defBufferSize =
        Convert.ToInt32(FileLengthConverter.ConvertFileLength(128, LengthType.Kilobytes, LengthType.Bytes, 0));

    /// <summary>
    /// Compares the size.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="otherFileSize">Size of the other file.</param>
    /// <returns>
    ///     <para>Less than zero - This instance is smaller than the other.</para>
    ///     <para>Zero - This instance is equal by size to the other.</para>
    ///     <para>Greater than zero - This instance is bigger than the other.</para>
    /// </returns>
    public static int CompareSize(this FileInfo file, long otherFileSize) =>
        file.Length < otherFileSize ? -1 : file.Length > otherFileSize ? 1 : 0;

    /// <summary>
    /// Creates ZIP archive from file.
    /// </summary>
    /// <param name="file">The source file.</param>
    /// <param name="zipFilePath">
    /// The ZIP file path. If null - ZIP file will be created next to source file with the same name
    /// as original file.
    /// </param>
    /// <param name="deleteTempDirectory">if set to <c>true</c> [delete temporary directory].</param>
    /// <param name="overwrite">if set to <c>true</c> [overwrite].</param>
    /// <returns></returns>
    public static async Task<FileInfo> ZipAsync(this FileInfo file,
                                                string? zipFilePath = null,
                                                bool deleteTempDirectory = false,
                                                bool overwrite = false) =>
        await file.ZipAsync(zipFilePath is not null
                ? new FileInfo(zipFilePath)
                : null,
            deleteTempDirectory,
            overwrite);

    /// <summary>Compresses a file into a zip archive containing only that file.</summary>
    /// <param name="file">The file to compress.</param>
    /// <param name="zipFile">The archive to create; <c>&lt;file name&gt;.zip</c> next to the file when null.</param>
    /// <param name="deleteTempDirectory">Whether an existing temporary working directory is deleted first.</param>
    /// <param name="overwrite">Whether an existing archive is replaced.</param>
    /// <returns>The created archive.</returns>
    public static async Task<FileInfo> ZipAsync(this FileInfo file,
                                                FileInfo? zipFile = null,
                                                bool deleteTempDirectory = false,
                                                bool overwrite = false)
    {
        var parentDirectory = (zipFile is null
            ? file.Directory
            : zipFile.Directory).Guard("parentDirectory");

        var tempDirectory =
            new DirectoryInfo(Path.Combine(parentDirectory.FullName, Path.GetFileNameWithoutExtension(file.Name)));

        if (tempDirectory.Exists && deleteTempDirectory)
            tempDirectory.Delete(true);

        tempDirectory.Create();
        var targetFilePath = Path.Combine(tempDirectory.FullName, file.Name);

#if NETSTANDARD2_0
        using (var sourceStream = file.OpenRead())
        using (var targetStream = File.Open(targetFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
#else
        await using (var sourceStream = file.OpenRead())
        await using (var targetStream =
                     File.Open(targetFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
#endif

            await sourceStream.CopyToAsync(targetStream);

        zipFile ??= new(Path.Combine(parentDirectory.FullName, $"{file.Name}.zip"));

        if (zipFile.Exists && overwrite)
        {
            zipFile.Delete();
            zipFile.Refresh();
        }
#if NETSTANDARD
        ZipFile.CreateFromDirectory(tempDirectory.FullName, zipFile.FullName, CompressionLevel.Optimal, false);
#else
        await ZipFile.CreateFromDirectoryAsync(tempDirectory.FullName,
            zipFile.FullName,
            CompressionLevel.Optimal,
            false);
#endif
        tempDirectory.Delete(true);
        zipFile.Refresh();

        return zipFile;
    }

    /// <summary>Computes the MD5 hash of a file's content.</summary>
    /// <param name="file">The file to hash.</param>
    /// <param name="removeDashes">Whether to remove the dashes between hex bytes.</param>
    /// <param name="toLower">Whether to lower-case the hex text.</param>
    /// <param name="asBase64String">Whether to return Base64 instead of hex.</param>
    /// <returns>The formatted hash, or null when the file does not exist.</returns>
    public static string? GenerateMd5OfFile(this FileInfo file,
                                           bool removeDashes = true,
                                           bool toLower = true,
                                           bool asBase64String = false)
    {
        file.Refresh();

        byte[]? hash = null;

        if (file.Exists)
            using (var stream = new FileStream(file.FullName,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.ReadWrite,
                       _defBufferSize))
            using (var md5 = MD5.Create())
                hash = md5.ComputeHash(stream);

        return hash.GetHashString(removeDashes, toLower, asBase64String);
    }

    /// <summary>Determines whether a file is on an NTFS volume.</summary>
    /// <param name="file">The file to test.</param>
    /// <returns><c>true</c> if the volume uses NTFS.</returns>
    public static bool IsNtfs(this FileInfo file) => FileSystemHelper.IsPathNtfs(file.FullName);

    /// <summary>
    /// Computes the md5 hash.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="removeDashes">if set to <c>true</c> [remove dashes].</param>
    /// <param name="toLower">if set to <c>true</c> [to lower].</param>
    /// <param name="asBase64String">if set to <c>true</c> [as base64 string].</param>
    public static string ComputeMd5Hash(this byte[] data,
                                        bool removeDashes = true,
                                        bool toLower = true,
                                        bool asBase64String = false)
    {
#if NETSTANDARD
        using var md5Algorithm = MD5.Create();
        var hash = md5Algorithm.ComputeHash(data);
#else
        var hash = MD5.HashData(data);
#endif

        return hash.GetHashString(removeDashes, toLower, asBase64String);
    }

    /// <summary>Reads a file into a memory stream.</summary>
    /// <param name="file">The file to read.</param>
    /// <returns>A memory stream positioned at the start, or null when the file does not exist.</returns>
    public static async Task<MemoryStream?> ToMemoryStreamAsync(this FileInfo file)
    {
        file.Refresh();

        return !file.Exists
            ? null
#pragma warning disable IDISP004
            : await new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, _defBufferSize)
#pragma warning restore IDISP004
                .CopyToMemoryStreamAsync(true);
    }
}