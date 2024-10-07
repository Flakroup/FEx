// -----------------------------------------------------------------------
// <copyright file="ShellIcon.cs" company="Mauricio DIAZ ORLICH (madd0@madd0.com)">
//   Distributed under Microsoft Public License (MS-PL).
//   http://www.opensource.org/licenses/MS-PL
// </copyright>
// -----------------------------------------------------------------------

using System.Drawing;
using System.Runtime.InteropServices;

namespace FEx.WPFx.Natives;

/// <summary>
///     Get a small or large Icon with an easy C# function call
///     that returns a 32x32 or 16x16 System.Drawing.Icon depending on which function you call
///     either GetSmallIcon(string fileName) or GetLargeIcon(string fileName)
/// </summary>
public static class ShellIcon
{
    public static Icon GetSmallFolderIcon() => GetIcon("folder", SHGFI.SmallIcon | SHGFI.UseFileAttributes, true);

    public static Icon GetLargeFolderIcon() => GetIcon("folder", SHGFI.LargeIcon | SHGFI.UseFileAttributes, true);

    public static Icon GetSmallIcon(string fileName) => GetIcon(fileName, SHGFI.SmallIcon);

    public static Icon GetLargeIcon(string fileName) => GetIcon(fileName, SHGFI.LargeIcon);

    public static Icon GetSmallIconFromExtension(string extension) =>
        GetIcon(extension, SHGFI.SmallIcon | SHGFI.UseFileAttributes);

    public static Icon GetLargeIconFromExtension(string extension) =>
        GetIcon(extension, SHGFI.LargeIcon | SHGFI.UseFileAttributes);

    private static Icon GetIcon(string fileName, SHGFI flags, bool isFolder = false)
    {
        var shinfo = new SHFileInfo();

        _ = Win32.GetFileInfo(fileName,
            isFolder
                ? FileAttributeDirectory
                : FileAttributeNormal,
            ref shinfo,
            (uint)Marshal.SizeOf(shinfo),
            (uint)(SHGFI.Icon | flags));

        var icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
        Win32.DestroySHIcon(shinfo);

        return icon;
    }

    #region Interop constants
    private const uint FileAttributeNormal = 0x80;
    private const uint FileAttributeDirectory = 0x10;
    #endregion
}