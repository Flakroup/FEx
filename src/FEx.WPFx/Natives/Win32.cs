// -----------------------------------------------------------------------
// <copyright file="ShellIcon.cs" company="Mauricio DIAZ ORLICH (madd0@madd0.com)">
//   Distributed under Microsoft Public License (MS-PL).
//   http://www.opensource.org/licenses/MS-PL
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace FEx.WPFx.Natives;

public static class Win32
{
    public static IntPtr GetFileInfo(string pszPath,
                                     uint dwFileAttributes,
                                     ref SHFileInfo psfi,
                                     uint cbSizeFileInfo,
                                     uint uFlags) =>
        SHGetFileInfo(pszPath, dwFileAttributes, ref psfi, cbSizeFileInfo, uFlags);

    public static void DestroySHIcon(SHFileInfo shinfo) => DestroyIcon(shinfo.hIcon);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string pszPath,
                                               uint dwFileAttributes,
                                               ref SHFileInfo psfi,
                                               uint cbSizeFileInfo,
                                               uint uFlags);

    [DllImport("User32.dll", CharSet = CharSet.Unicode)]
    private static extern int DestroyIcon(IntPtr hIcon);
}