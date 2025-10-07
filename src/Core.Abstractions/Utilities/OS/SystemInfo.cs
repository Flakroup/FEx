using System.Runtime.InteropServices;

/* Unmerged change from project 'FEx.Common (netstandard2.0)'
Before:
using FEx.Common.Utilities.OS;
After:
using FEx;
using FEx.Basics;
using FEx.Basics.Utilities;
using FEx.Common.Utilities.OS;
*/

namespace FEx.Common.Utilities.OS;

[StructLayout(LayoutKind.Sequential)]
public struct SystemInfo
{
    internal ProcessorInfoUnion uProcessorInfo;
    public uint dwPageSize;
    public nint lpMinimumApplicationAddress;
    public nint lpMaximumApplicationAddress;
    public nint dwActiveProcessorMask;
    public uint dwNumberOfProcessors;
    public uint dwProcessorType;
    public uint dwAllocationGranularity;
    public ushort dwProcessorLevel;
    public ushort dwProcessorRevision;
}