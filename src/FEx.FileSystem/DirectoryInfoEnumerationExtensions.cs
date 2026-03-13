#if NETSTANDARD2_0 || NETSTANDARD2_1
using System.Collections.Generic;
using System.IO;

namespace FEx.FileSystem;

/// <summary>
/// Shim extension methods that allow the rest of the codebase (which expects <see cref="EnumerationOptions" />)
/// to compile and work on older frameworks where that type does not exist (e.g. netstandard2.0/2.1).
/// The implementation maps the subset of functionality that is possible to emulate via the existing
/// <see cref="SearchOption" />
/// overloads. Properties that have no equivalent are ignored.
/// </summary>
internal static class DirectoryInfoEnumerationExtensions
{
    public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo dir,
                                                       string searchPattern,
                                                       FExEnumerationOptions options) =>
        dir.EnumerateFiles(searchPattern, GetSearchOption(options));

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo dir,
                                                                  string searchPattern,
                                                                  FExEnumerationOptions options) =>
        dir.EnumerateDirectories(searchPattern, GetSearchOption(options));

    public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo dir,
                                                                       string searchPattern,
                                                                       FExEnumerationOptions options) =>
        dir.EnumerateFileSystemInfos(searchPattern, GetSearchOption(options));

    public static DirectoryInfo[] GetDirectories(this DirectoryInfo dir,
                                                 string searchPattern,
                                                 FExEnumerationOptions options) =>
        dir.GetDirectories(searchPattern, GetSearchOption(options));

    private static SearchOption GetSearchOption(FExEnumerationOptions options) =>
        options?.RecurseSubdirectories == true
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;
}
#endif