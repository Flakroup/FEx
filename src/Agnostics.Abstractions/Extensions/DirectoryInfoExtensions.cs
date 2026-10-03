using FEx.Agnostics.Abstractions.Extensions.Collections.Dictionaries;
using FEx.Agnostics.Abstractions.Helpers;
using FEx.Agnostics.Abstractions.IO;
using System;
using System.IO;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for building paths below a directory and for resolving special folders.</summary>
public static class DirectoryInfoExtensions
{
    /// <summary>Determines whether a directory is on an NTFS volume</summary>
    /// <param name="dir">The directory to test.</param>
    /// <returns><c>true</c> if the volume uses NTFS.</returns>
    public static bool IsNtfs(this DirectoryInfo dir) => FileSystemHelper.IsPathNtfs(dir.FullName);

    /// <summary>Combines a directory with descendant path segments, adding the <c>\\?\</c> long-path prefix when a path would exceed 260 characters</summary>
    /// <param name="dir">The base directory.</param>
    /// <param name="descendants">The path segments to append.</param>
    /// <returns>The combined path.</returns>
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

    /// <summary>Gets a file below a directory, creating its parent directory when missing</summary>
    /// <param name="dir">The base directory.</param>
    /// <param name="descendants">The path segments leading to the file.</param>
    /// <returns>The file.</returns>
    public static FileInfo GetDescendantFile(this DirectoryInfo dir, params string[] descendants) =>
        dir.GetDescendantFileSystemObject(path =>
            {
                var file = new FileInfo(path);
                file.Directory?.Create();

                return file;
            },
            descendants);

    /// <summary>Gets a directory below another directory, creating it when missing</summary>
    /// <param name="dir">The base directory.</param>
    /// <param name="descendants">The path segments leading to the directory.</param>
    /// <returns>The directory.</returns>
    public static DirectoryInfo GetDescendantDirectory(this DirectoryInfo dir, params string[] descendants) =>
        dir.GetDescendantFileSystemObject(path =>
            {
                var directory = new DirectoryInfo(path);
                directory.Create();

                return directory;
            },
            descendants);

    /// <summary>Creates a file system object for a descendant path of a directory path</summary>
    /// <typeparam name="T">The type of object created.</typeparam>
    /// <param name="directoryPath">The base directory path.</param>
    /// <param name="activator">Creates the object from the combined path.</param>
    /// <param name="descendants">The path segments to append.</param>
    /// <returns>The created object.</returns>
    public static T GetDescendantFileSystemObject<T>(this string directoryPath,
                                                     Func<string, T> activator,
                                                     params string[] descendants) =>
        new DirectoryInfo(directoryPath).GetDescendantFileSystemObject(activator, descendants);

    /// <summary>Creates a file system object for a descendant path of a directory</summary>
    /// <typeparam name="T">The type of object created.</typeparam>
    /// <param name="dir">The base directory.</param>
    /// <param name="activator">Creates the object from the combined path.</param>
    /// <param name="descendants">The path segments to append.</param>
    /// <returns>The created object.</returns>
    public static T GetDescendantFileSystemObject<T>(this DirectoryInfo dir,
                                                     Func<string, T> activator,
                                                     params string[] descendants) =>
        activator(dir.GetDescendantPath(descendants));

    /// <summary>Combines the path of a special folder with descendant path segments</summary>
    /// <param name="folder">The special folder.</param>
    /// <param name="descendants">The path segments to append.</param>
    /// <returns>The combined path.</returns>
    public static string GetSpecialDirectoryPathDescendants(this Environment.SpecialFolder folder,
                                                            params string[] descendants) =>
        folder.GetSpecialDirectory().Directory.GetDescendantPath(descendants);

    /// <summary>Gets the special directory for a special folder</summary>
    /// <param name="folder">The special folder.</param>
    /// <returns>The special directory.</returns>
    public static SpecialDirectory GetSpecialDirectory(this Environment.SpecialFolder folder) =>
        SpecialDirectory.SpecialDirectories.TryGetReadOnlyKeyValue<Environment.SpecialFolder, SpecialDirectory>(folder);
}