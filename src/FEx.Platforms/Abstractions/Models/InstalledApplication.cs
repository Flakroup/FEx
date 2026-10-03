using Microsoft.Win32;
using System;

namespace FEx.Platforms.Abstractions.Models;

/// <summary>
/// Immutable snapshot of an "Uninstall" registry entry. Holds no registry handle.
/// </summary>
/// <param name="KeyName">Name of the entry's subkey under the Uninstall key.</param>
/// <param name="DisplayName">Application name; <c>null</c> for entries that have none.</param>
/// <param name="DisplayVersion">Application version as written by its installer.</param>
/// <param name="Publisher">Publisher name.</param>
/// <param name="InstallLocation">Installation directory, when the installer recorded one.</param>
/// <param name="UninstallString">Command line that uninstalls the application.</param>
/// <param name="InstallDate">Installation date, when the installer recorded a valid one.</param>
/// <param name="Hive">Registry hive the entry was read from.</param>
/// <param name="View">Registry view the entry was read from.</param>
public sealed record InstalledApplication(
    string KeyName,
    string? DisplayName,
    string? DisplayVersion,
    string? Publisher,
    string? InstallLocation,
    string? UninstallString,
    DateTime? InstallDate,
    RegistryHive Hive,
    RegistryView View);
