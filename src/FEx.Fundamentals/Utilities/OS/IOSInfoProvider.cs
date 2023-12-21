using FEx.Fundamentals.Utilities.OS.Enums;
using System;

namespace FEx.Fundamentals.Utilities.OS;

public interface IOSInfoProvider
{
    string InfoString { get; }

    /// <summary>
    ///     Determines if the current application is 32 or 64-bit.
    /// </summary>
    SoftwareArchitecture ProgramBits { get; }

    SoftwareArchitecture OSBits { get; }

    /// <summary>
    ///     Determines if the current processor is 32 or 64-bit.
    /// </summary>
    OSProcessorArchitecture ProcessorBits { get; }

    OSEdition Edition { get; }

    /// <summary>
    ///     Gets the name of the operating system running on this computer.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the service pack information of the operating system running on this computer.
    /// </summary>
    string ServicePack { get; }

    /// <summary>
    ///     Gets the build version number of the operating system running on this computer.
    /// </summary>
    int BuildVersion { get; }

    /// <summary>
    ///     Gets the full version of the operating system running on this computer.
    /// </summary>
    Version Version { get; }

    bool Is64BitOperatingSystem { get; }

    /// <summary>
    ///     Gets the edition of the operating system running on this computer.
    /// </summary>
    string EditionString { get; }
}