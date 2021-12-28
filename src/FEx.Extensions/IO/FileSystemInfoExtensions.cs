namespace FEx.Extensions.IO;

public static class FileSystemInfoExtensions
{
    public static DirectoryInfo GetDirectory(this FileSystemInfo fileSystemInfo)
    {
        switch (fileSystemInfo)
        {
            case DirectoryInfo info:
                return info;
            case FileInfo fileInfo:
                return fileInfo.Directory;
            default:
                return fileSystemInfo.IsPathFile()
                    ? new FileInfo(fileSystemInfo.FullName).Directory
                    : new DirectoryInfo(fileSystemInfo.FullName);
        }
    }

    public static bool IsPathFile(this FileSystemInfo fileSystemInfo)
    {
        return !fileSystemInfo.Attributes.HasFlag(FileAttributes.Directory);
    }
}