using System;
using System.Runtime.InteropServices;

namespace FEx.Fundamentals.Utilities.OS;

[StructLayout(LayoutKind.Sequential)]
public struct SystemInfo
{
    internal ProcessorInfoUnion uProcessorInfo;
    public uint dwPageSize;
    public IntPtr lpMinimumApplicationAddress;
    public IntPtr lpMaximumApplicationAddress;
    public IntPtr dwActiveProcessorMask;
    public uint dwNumberOfProcessors;
    public uint dwProcessorType;
    public uint dwAllocationGranularity;
    public ushort dwProcessorLevel;
    public ushort dwProcessorRevision;
}