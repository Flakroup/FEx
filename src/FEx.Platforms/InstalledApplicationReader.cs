using FEx.Platforms.Abstractions.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace FEx.Platforms;

/// <summary>
/// An opened "Uninstall" entry; disposing it releases the underlying registry handle.
/// </summary>
internal interface IUninstallEntry : IDisposable
{
    object? GetValue(string name);
}

internal static class InstalledApplicationReader
{
    private const string InstallDateFormat = "yyyyMMdd";

    /// <summary>
    /// Opens every entry, maps it to an <see cref="InstalledApplication" /> and disposes it before moving on,
    /// so no handle outlives this call.
    /// </summary>
    public static List<InstalledApplication> ReadAll(IEnumerable<string> keyNames,
                                                     Func<string, IUninstallEntry?> open,
                                                     RegistryHive hive,
                                                     RegistryView view)
    {
        var applications = new List<InstalledApplication>();

        foreach (var keyName in keyNames)
        {
            using var entry = open(keyName);

            if (entry is not null)
                applications.Add(Map(keyName, entry.GetValue, hive, view));
        }

        return applications;
    }

    public static InstalledApplication Map(string keyName,
                                           Func<string, object?> getValue,
                                           RegistryHive hive,
                                           RegistryView view) =>
        new(keyName,
            GetString(getValue, "DisplayName"),
            GetString(getValue, "DisplayVersion"),
            GetString(getValue, "Publisher"),
            GetString(getValue, "InstallLocation"),
            GetString(getValue, "UninstallString"),
            ParseInstallDate(GetString(getValue, "InstallDate")),
            hive,
            view);

    private static string? GetString(Func<string, object?> getValue, string name)
    {
        var value = getValue(name)?.ToString();

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }

    private static DateTime? ParseInstallDate(string? value) =>
        DateTime.TryParseExact(value,
            InstallDateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
}
