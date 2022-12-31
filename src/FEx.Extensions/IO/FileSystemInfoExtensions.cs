using System.IO;

namespace FEx.Extensions.IO;

public static class FileSystemInfoExtensions
{
    public static DirectoryInfo GetDirectory(this FileSystemInfo fileSystemInfo)
    {
        return fileSystemInfo switch
        {
            DirectoryInfo info => info,
            FileInfo fileInfo => fileInfo.Directory,
            _ => fileSystemInfo.IsPathFile()
                ? new FileInfo(fileSystemInfo.FullName).Directory
                : new(fileSystemInfo.FullName)
        };
    }

    public static bool IsPathFile(this FileSystemInfo fileSystemInfo)
    {
        return !fileSystemInfo.Attributes.HasFlag(FileAttributes.Directory);
    }
}