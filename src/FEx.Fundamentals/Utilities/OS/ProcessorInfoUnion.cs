using System.Runtime.InteropServices;

namespace FEx.Fundamentals.Utilities.OS;

[StructLayout(LayoutKind.Explicit)]
public struct ProcessorInfoUnion
{
    [FieldOffset(0)] internal uint dwOemId;

    [FieldOffset(0)] internal ushort wProcessorArchitecture;

    [FieldOffset(2)] internal ushort wReserved;
}