// -----------------------------------------------------------------------
// <copyright file="ShellIcon.cs" company="Mauricio DIAZ ORLICH (madd0@madd0.com)">
//   Distributed under Microsoft Public License (MS-PL).
//   http://www.opensource.org/licenses/MS-PL
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace FEx.WPFx.Natives;

[StructLayout(LayoutKind.Sequential)]
public struct SHFileInfo
{
    public IntPtr hIcon;
    public IntPtr iIcon;
    public uint dwAttributes;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string szDisplayName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    public string szTypeName;
}