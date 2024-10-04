using FEx.Common.Extensions;
using FEx.Extensions.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FEx.Extensions.Base.IO;

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
    ///     Determines whether [is path NTFS] [the specified absolute file path].
    /// </summary>
    /// <param name="absolutePath">The absolute path.</param>
    /// <returns>
    ///     <c>true</c> if [is path NTFS] [the specified absolute file path]; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsPathNtfs(string absolutePath)
    {
        string pathRoot = Path.GetPathRoot(absolutePath);

        if (pathRoot is not null)
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            DriveInfo driveBasedOnPath = allDrives.FindInEnumerable(d => d.RootDirectory.Name == pathRoot);

            return driveBasedOnPath?.DriveFormat.IsEqual(Ntfs) == true
                   && driveBasedOnPath.DriveType.IsIn(DriveType.Fixed, DriveType.Removable);
        }

        return false;
    }

    /// <summary>
    ///     Generates the md5 of file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns></returns>
    public static string GenerateMd5OfFile(string filePath) => new FileInfo(filePath).GenerateMd5OfFile();

    public static string GetParentFolderFromPath(string path, char pathSeparator, bool includeSeparatorAtEnd)
    {
        int pos = path.TrimEnd(pathSeparator).LastIndexOf(pathSeparator.ToString(), StringComparison.Ordinal);
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
        char[] toReplace = path.Distinct().Where(x => InvalidPathChars.Contains(x)).ToArray();

        if (toReplace.Length > 0)
        {
            var sb = new StringBuilder(path);

            foreach (char c in toReplace)
                sb.Replace(c, '_');

            path = sb.ToString();
        }

        string[] segments = path.Split(Path.DirectorySeparatorChar);

        for (var i = 0; i < segments.Length; i++)
        {
            char[] toBeReplaced = segments[i].Distinct().Where(x => InvalidFileOrDirNameChars.Contains(x)).ToArray();

            if (toBeReplaced.Length > 0)
                foreach (char c in toBeReplaced)
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