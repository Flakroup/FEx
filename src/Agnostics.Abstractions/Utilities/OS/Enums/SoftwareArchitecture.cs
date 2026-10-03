namespace FEx.Agnostics.Abstractions.Utilities.OS.Enums;

/// <summary>Bitness of a running process or operating system.</summary>
public enum SoftwareArchitecture
{
    /// <summary>The bitness could not be determined.</summary>
    Unknown = 0,
    /// <summary>32-bit.</summary>
    Bit32 = 1,
    /// <summary>64-bit.</summary>
    Bit64 = 2
}