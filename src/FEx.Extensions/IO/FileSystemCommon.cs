using FEx.Extensions.Collections.Enumerables;

namespace FEx.Extensions.IO;

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

        if (pathRoot != null)
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            DriveInfo driveBasedOnPath = allDrives.Find(d => d.RootDirectory.Name == pathRoot);
            return driveBasedOnPath?.DriveFormat.EqualsIgnoreCase(Ntfs) == true && driveBasedOnPath.DriveType.IsIn(DriveType.Fixed, DriveType.Removable);
        }

        return false;
    }

    /// <summary>
    ///     Generates the md5 of file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns></returns>
    public static string GenerateMd5OfFile(string filePath)
    {
        return new FileInfo(filePath).GenerateMd5OfFile();
    }
}