using System.Runtime.InteropServices;

namespace FEx.Core.Abstractions.Utilities.OS;

[StructLayout(LayoutKind.Sequential)]
internal struct OSVersionInfoEx
{
    public int dwOSVersionInfoSize;
    public int dwMajorVersion;
    public int dwMinorVersion;
    public int dwBuildNumber;
    public int dwPlatformId;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string szCSDVersion;

    public short wServicePackMajor;
    public short wServicePackMinor;
    public short wSuiteMask;
    public byte wProductType;
    public byte wReserved;
}