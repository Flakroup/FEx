using System.Runtime.InteropServices;

namespace FEx.Agnostics.Abstractions.Utilities.OS;

/// <summary>Native <c>SYSTEM_INFO</c> union that overlays the OEM id with the processor architecture.</summary>
[StructLayout(LayoutKind.Explicit)]
public struct ProcessorInfoUnion
{
    [FieldOffset(0)]
    internal uint dwOemId;

    [FieldOffset(0)]
    internal ushort wProcessorArchitecture;

    [FieldOffset(2)]
    internal ushort wReserved;
}