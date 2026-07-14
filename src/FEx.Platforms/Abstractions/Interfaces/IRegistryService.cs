using FEx.Agnostics.Abstractions.Enums;
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace FEx.Platforms.Abstractions.Interfaces;

public interface IRegistryService
{
    List<RegistryKey> GetInstalledApplications();
    List<Version> GetVersionFromRegistry();
    List<Version>? Get45PlusFromRegistry();
    RegistryKey? GetClassesRootSubKey(string subKey, bool writable);
    RegistryKey? GetLocalMachineSubKey(string subKey, bool writable);
    RegistryKey? GetCurrentUserSubKey(string subKey, bool writable);
    RegistryKey? GetSubKey(RegistryKey registry, string subKey, bool writable);
    RegistryKey GetOrAddCurrentUserSubKey(string subKey, bool writable);
    RegistryKey GetOrAddLocalMachineSubKey(string subKey, bool writable);
    void SetStartup(string appName, string executablePath, bool enable, bool global);
    string GetStandardBrowserPath();
    string? GetDefaultExtension(string? mimeType);
    string? GetDefaultExtension(MediaTypes mediaType);
    string? GetDefaultMimeType(string extension);
    string GetOrAddRegistryKeyStringValue(string path, string keyName, Func<string> getNewValue);
}