using System.IO;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for file system entries.</summary>
public static class FileSystemInfoExtensions
{
    /// <summary>Gets the directory of a file system entry.</summary>
    /// <param name="fileSystemInfo">The entry.</param>
    /// <returns>The directory itself, the directory containing a file, or the path as a directory when the entry type is unknown.</returns>
    public static DirectoryInfo? GetDirectory(this FileSystemInfo fileSystemInfo) =>
        fileSystemInfo switch
        {
            DirectoryInfo info => info,
            FileInfo fileInfo => fileInfo.Directory,
            _ => fileSystemInfo.IsPathFile()
                ? new FileInfo(fileSystemInfo.FullName).Directory
                : new(fileSystemInfo.FullName)
        };

    /// <summary>Determines whether an entry is not a directory.</summary>
    /// <param name="fileSystemInfo">The entry to test.</param>
    /// <returns><c>true</c> if the directory attribute is not set.</returns>
    public static bool IsPathFile(this FileSystemInfo fileSystemInfo) =>
        !fileSystemInfo.Attributes.HasFlag(FileAttributes.Directory);
}