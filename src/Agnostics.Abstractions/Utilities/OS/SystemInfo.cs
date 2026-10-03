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

namespace FEx.Agnostics.Abstractions.Utilities.OS;

/// <summary>Managed layout of the native Win32 <c>SYSTEM_INFO</c> structure filled by <c>GetSystemInfo</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct SystemInfo
{
    internal ProcessorInfoUnion uProcessorInfo;
    /// <summary>The page size and the granularity of page protection and commitment, in bytes.</summary>
    public uint dwPageSize;
    /// <summary>Pointer to the lowest memory address accessible to applications and DLLs.</summary>
    public nint lpMinimumApplicationAddress;
    /// <summary>Pointer to the highest memory address accessible to applications and DLLs.</summary>
    public nint lpMaximumApplicationAddress;
    /// <summary>Bit mask of the processors configured into the system.</summary>
    public nint dwActiveProcessorMask;
    /// <summary>The number of logical processors in the current group.</summary>
    public uint dwNumberOfProcessors;
    /// <summary>Obsolete processor type code (for example 586 for Pentium-class).</summary>
    public uint dwProcessorType;
    /// <summary>The granularity for the starting address at which virtual memory can be allocated, in bytes.</summary>
    public uint dwAllocationGranularity;
    /// <summary>The architecture-dependent processor level.</summary>
    public ushort dwProcessorLevel;
    /// <summary>The architecture-dependent processor revision.</summary>
    public ushort dwProcessorRevision;
}