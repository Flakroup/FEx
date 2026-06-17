using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace FEx.Legacy.IO;

// CA1416: This type is Windows-only by design - it resolves mapped network drives via WMI
// (System.Management). CA1416 fires only on the net10.0 target (cross-platform); netstandard
// targets do not run the platform analyzer. Suppressed rather than annotated because
// SupportedOSPlatformAttribute is unavailable on the netstandard2.0 BCL.
#pragma warning disable CA1416

/// <summary>
/// A static class to help with resolving a mapped drive path to a UNC network path.
/// If a local drive path or a UNC network path are passed in, they will just be returned.
/// </summary>
/// <example>
/// using System;
/// using System.IO;
/// using System.Management;    // Reference System.Management.dll
/// // Example/Test paths, these will need to be adjusted to match your environment.
/// string[] paths = new string[] {
/// @"Z:\ShareName\Sub-Folder",
/// @"\\ACME-FILE\ShareName\Sub-Folder",
/// @"\\ACME.COM\ShareName\Sub-Folder", // DFS
/// @"C:\Temp",
/// @"\\localhost\c$\temp",
/// @"\\workstation\Temp",
/// @"Z:", // Mapped drive pointing to \\workstation\Temp
/// @"C:\",
/// @"Temp",
/// @".\Temp",
/// @"..\Temp",
/// "",
/// "    ",
/// null
/// };
/// foreach (var curPath in paths) {
/// try {
/// Console.WriteLine($"{curPath} = {MappedDriveResolver.ResolveToUNC(curPath)}");
/// }
/// catch (Exception ex) {
/// Console.WriteLine($"{curPath} = {ex.Message}");
/// }
/// }
/// </example>
public static class MappedDriveResolver
{
    public static Dictionary<string, string> ReadMappedDrives()
    {
        var res = new Dictionary<string, string>();

        var p = Process.Start(new ProcessStartInfo
        {
            FileName = "net",
            Arguments = "use",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        });

        if (p is not null)
        {
            var str = p.StandardOutput.ReadToEnd();

            foreach (var s in str.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var s2 = s.Split([' '], StringSplitOptions.RemoveEmptyEntries);

                if (s2.Length >= 2
                    && s2[1][1] == ':')
                    res.Add(s2[1], s2[2]);
            }
        }

        p?.Dispose();

        return res;
    }

    /// <summary>
    /// Resolves the given path to a full UNC path if the path is a mapped drive.
    /// Otherwise, just returns the given path.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns></returns>
    public static string ResolveToUnc(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path), "The path argument was null or whitespace.");

        if (!Path.IsPathRooted(path))
            throw new ArgumentException(
                $"The path '{path}' was not a rooted path and ResolveToUNC does not support relative paths.");

        // Is the path already in the UNC format?
        if (path.StartsWith(@"\\"))
            return path;

        var rootPath = ResolveToRootUnc(path);

        if (path.StartsWith(rootPath))
            return path; // Local drive, no resolving occurred

        return path.Replace(GetDriveLetter(path), rootPath);
    }

    /// <summary>
    /// Given a local mapped drive letter, determine if it is a network drive. If so, return the server share.
    /// </summary>
    /// <param name="mappedDrive"></param>
    /// <returns>The server path that the drive maps to ~ "////XXXXXX//ZZZZ"</returns>
    public static string CheckUncPath(string mappedDrive)
    {
        //Query to return all the local computer's drives.
        //See http://msdn.microsoft.com/en-us/library/ms186146.aspx, or search "WMI Queries"
        var selectWmiQuery = new SelectQuery("Win32_LogicalDisk");
        using var driveSearcher = new ManagementObjectSearcher(selectWmiQuery);
        //Soem variables to be used inside and out of the foreach.
        var found = false;
        string serverName = null;
        using var disks = driveSearcher.Get();

        foreach (var disk in disks.Cast<ManagementObject>())
        {
            var path = disk.Path;

            if (path.ToString().Contains(mappedDrive))
            {
                using var networkDrive = new ManagementObject(path);

                if (Convert.ToUInt32(networkDrive["DriveType"]) == 4)
                {
                    serverName = Convert.ToString(networkDrive["ProviderName"]);
                    found = true;

                    break;
                }

                throw new DirectoryNotFoundException(
                    $"The drive {mappedDrive} was found, but is not a network drive. Were your network drives mapped correctly?");
            }
        }

        return !found
            ? throw new DirectoryNotFoundException(
                $"The drive {mappedDrive} was not found. Were your network drives mapped correctly?")
            : serverName;
    }

    /// <summary>
    /// Resolves the given path to a root UNC path if the path is a mapped drive.
    /// Otherwise, just returns the given path.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns></returns>
    public static string ResolveToRootUnc(string path)
    {
        if (!path.StartsWith(@"\\"))
        {
            var drive = GetDriveType(path);

            return drive.Item1 == DriveType.Network
                ? drive.Item3
                : drive.Item2 + Path.DirectorySeparatorChar;
        }

        return Directory.GetDirectoryRoot(path);
    }

    /// <summary>
    /// Checks if the given path is a network drive.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns></returns>
    public static bool IsNetworkDrive(string path)
    {
        if (!path.StartsWith(@"\\"))
        {
            var drive = GetDriveType(path);

            return drive.Item1 == DriveType.Network;
        }

        return true;
    }

    /// <summary>
    /// Given a path will extract just the drive letter with volume separator.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>C:</returns>
    public static string GetDriveLetter(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path), "The path argument was null or whitespace.");

        if (!Path.IsPathRooted(path))
            throw new ArgumentException(
                $"The path '{path}' was not a rooted path and GetDriveLetter does not support relative paths.");

        return path.StartsWith(@"\\")
            ? throw new ArgumentException("A UNC path was passed to GetDriveLetter")
            : Directory.GetDirectoryRoot(path).Replace(Path.DirectorySeparatorChar.ToString(), "");
    }

    private static (DriveType, string, string) GetDriveType(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path), "The path argument was null or whitespace.");

        if (!Path.IsPathRooted(path))
            throw new ArgumentException(
                $"The path '{path}' was not a rooted path and ResolveToRootUNC does not support relative paths.");

        // Get just the drive letter for WMI call
        var driveLetter = GetDriveLetter(path);
        //string unc = CheckUncPath(driveLetter);

        // Query WMI if the drive letter is a network drive
        using var mo = new ManagementObject();
        mo.Path = new($"Win32_LogicalDisk='{driveLetter}'");
        var networkRoot = Convert.ToString(mo["ProviderName"]);

        return ((DriveType)(uint)mo["DriveType"], driveLetter, networkRoot);
    }
}