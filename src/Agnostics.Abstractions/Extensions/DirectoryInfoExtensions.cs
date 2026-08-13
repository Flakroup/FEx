using FEx.Agnostics.Abstractions.Extensions.Collections.Dictionaries;
using FEx.Agnostics.Abstractions.Helpers;
using FEx.Agnostics.Abstractions.IO;
using System;
using System.IO;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class DirectoryInfoExtensions
{
    public static bool IsNtfs(this DirectoryInfo dir) => FileSystemHelper.IsPathNtfs(dir.FullName);

    public static string GetDescendantPath(this DirectoryInfo dir, params string[] descendants)
    {
        var path = dir.FullName;

        foreach (var d in descendants)
        {
            if (!path.StartsWith(@"\\?\")
                && path.Length + d.Length > 260)
                path = $@"\\?\{path}";

            path = Path.Combine(path, d);
        }

        return path;
    }

    public static FileInfo GetDescendantFile(this DirectoryInfo dir, params string[] descendants) =>
        dir.GetDescendantFileSystemObject(path =>
            {
                var file = new FileInfo(path);
                file.Directory?.Create();

                return file;
            },
            descendants);

    public static DirectoryInfo GetDescendantDirectory(this DirectoryInfo dir, params string[] descendants) =>
        dir.GetDescendantFileSystemObject(path =>
            {
                var directory = new DirectoryInfo(path);
                directory.Create();

                return directory;
            },
            descendants);

    public static T GetDescendantFileSystemObject<T>(this string directoryPath,
                                                     Func<string, T> activator,
                                                     params string[] descendants) =>
        new DirectoryInfo(directoryPath).GetDescendantFileSystemObject(activator, descendants);

    public static T GetDescendantFileSystemObject<T>(this DirectoryInfo dir,
                                                     Func<string, T> activator,
                                                     params string[] descendants) =>
        activator(dir.GetDescendantPath(descendants));

    public static string GetSpecialDirectoryPathDescendants(this Environment.SpecialFolder folder,
                                                            params string[] descendants) =>
        folder.GetSpecialDirectory().Directory.GetDescendantPath(descendants);

    public static SpecialDirectory GetSpecialDirectory(this Environment.SpecialFolder folder) =>
        SpecialDirectory.SpecialDirectories.TryGetReadOnlyKeyValue<Environment.SpecialFolder, SpecialDirectory>(folder);
}