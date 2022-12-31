using System;
using System.IO;

namespace FEx.Extensions.IO;

public static class DirectoryInfoExtensions
{
    public static bool IsNtfs(this DirectoryInfo dir)
    {
        return FileSystemCommon.IsPathNtfs(dir.FullName);
    }

    public static string GetDescendantPath(this DirectoryInfo dir, params string[] descendants)
    {
        string path = dir.FullName;

        foreach (string d in descendants)
        {
            if (!path.StartsWith(@"\\?\")
                && path.Length + d.Length > 260)
                path = $@"\\?\{path}";

            path = Path.Combine(path, d);
        }

        return path;
    }

    public static FileInfo GetDescendantFile(this DirectoryInfo dir, params string[] descendants)
    {
        return GetDescendantFileSystemObject(dir, path =>
        {
            var file = new FileInfo(path);
            file.Directory.Create();
            return file;
        }, descendants);
    }

    public static DirectoryInfo GetDescendantDirectory(this DirectoryInfo dir, params string[] descendants)
    {
        return GetDescendantFileSystemObject(dir, path =>
        {
            var directory = new DirectoryInfo(path);
            directory.Create();
            return directory;
        }, descendants);
    }

    public static T GetDescendantFileSystemObject<T>(this string directoryPath, Func<string, T> activator, params string[] descendants)
    {
        return GetDescendantFileSystemObject(new DirectoryInfo(directoryPath), activator, descendants);
    }

    public static T GetDescendantFileSystemObject<T>(this DirectoryInfo dir, Func<string, T> activator, params string[] descendants)
    {
        return activator(GetDescendantPath(dir, descendants));
    }
}