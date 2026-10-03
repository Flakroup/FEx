namespace FEx.Agnostics.Abstractions.Utilities.OS.Enums;

/// <summary>Processor architecture of the operating system.</summary>
public enum OSProcessorArchitecture
{
    /// <summary>The architecture could not be determined.</summary>
    Unknown = 0,
    /// <summary>32-bit x86 processor.</summary>
    Bit32 = 1,
    /// <summary>64-bit x64 processor.</summary>
    Bit64 = 2,
    /// <summary>64-bit Intel Itanium processor.</summary>
    Itanium64 = 3
}