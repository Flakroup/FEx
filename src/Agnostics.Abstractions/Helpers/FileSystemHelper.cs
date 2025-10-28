using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FEx.Agnostics.Abstractions.Helpers;

public static class FileSystemHelper
{
    private const string Ntfs = "NTFS";
    private static char[] _invalidPathChars;
    private static char[] _invalidFileOrDirNameChars;
    public static char[] InvalidPathChars => _invalidPathChars ??= Path.GetInvalidPathChars();

    public static char[] InvalidFileOrDirNameChars => _invalidFileOrDirNameChars ??= Path.GetInvalidFileNameChars();

    public static HashSet<char> InvalidFileNameChars { get; } =
    [
        ..InvalidFileOrDirNameChars.Concat(InvalidPathChars).Distinct()
    ];

    /// <summary>
    /// Determines whether [is path NTFS] [the specified absolute file path].
    /// </summary>
    /// <param name="absolutePath">The absolute path.</param>
    /// <returns>
    /// <c>true</c> if [is path NTFS] [the specified absolute file path]; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsPathNtfs(string absolutePath)
    {
        var pathRoot = Path.GetPathRoot(absolutePath);

        if (pathRoot is not null)
        {
            var allDrives = DriveInfo.GetDrives();
            var driveBasedOnPath = allDrives.FindInEnumerable(d => d.RootDirectory.Name == pathRoot);

            return driveBasedOnPath?.DriveFormat.IsEqual(Ntfs) == true
                   && driveBasedOnPath.DriveType.IsIn(DriveType.Fixed, DriveType.Removable);
        }

        return false;
    }

    /// <summary>
    /// Generates the md5 of file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns></returns>
    public static string GenerateMd5OfFile(string filePath) => new FileInfo(filePath).GenerateMd5OfFile();

    public static string GetParentFolderFromPath(string path, char pathSeparator, bool includeSeparatorAtEnd)
    {
        var pos = path.TrimEnd(pathSeparator).LastIndexOf(pathSeparator.ToString(), StringComparison.Ordinal);
#if NETSTANDARD
        return path.Substring(includeSeparatorAtEnd
            ? pos + 1
            : pos);
#else
        return path[..(includeSeparatorAtEnd
            ? pos + 1
            : pos)];
#endif
    }

    public static string FixPath(string path)
    {
        char[] toReplace = [.. path.Distinct().Where(x => InvalidPathChars.Contains(x))];

        if (toReplace.Length > 0)
        {
            var sb = new StringBuilder(path);

            foreach (var c in toReplace)
                sb.Replace(c, '_');

            path = sb.ToString();
        }

        var segments = path.Split(Path.DirectorySeparatorChar);

        for (var i = 0; i < segments.Length; i++)
        {
            char[] toBeReplaced = [.. segments[i].Distinct().Where(x => InvalidFileOrDirNameChars.Contains(x))];

            if (toBeReplaced.Length > 0)
                foreach (var c in toBeReplaced)
                    segments[i] = segments[i].Replace(c, '_');
        }

#if NETSTANDARD
        path = string.Join(Path.DirectorySeparatorChar.ToString(), segments);
#else
        path = string.Join(Path.DirectorySeparatorChar, segments);
#endif
        return path;
    }
}