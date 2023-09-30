using FEx.Extensions.Collections.Enumerables;
using FEx.Extensions.IO;
using System;
using System.IO;

namespace FEx.Extensions.Base;

public static class FileSystemCommon
{
    private const string Ntfs = "NTFS";

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
}