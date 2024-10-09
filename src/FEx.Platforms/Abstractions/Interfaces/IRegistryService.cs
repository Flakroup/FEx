using FEx.Extensions.Base.Enums;
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace FEx.Platforms.Abstractions.Interfaces;

public interface IRegistryService
{
    List<RegistryKey> GetInstalledApplications();
    List<Version> GetVersionFromRegistry();
    List<Version> Get45PlusFromRegistry();
    RegistryKey GetClassesRootSubKey(string subKey, bool writable = true);
    RegistryKey GetLocalMachineSubKey(string subKey, bool writable = true);
    RegistryKey GetCurrentUserSubKey(string subKey, bool writable = true);
    RegistryKey GetSubKey(RegistryKey registry, string subKey, bool writable = true);
    RegistryKey GetOrAddCurrentUserSubKey(string subKey, bool writable = true);
    RegistryKey GetOrAddLocalMachineSubKey(string subKey, bool writable = true);
    void SetStartup(string appName, string executablePath, bool enable, bool global = false);
    string GetStandardBrowserPath();
    string GetDefaultExtension(string mimeType);
    string GetDefaultExtension(MediaTypes mediaType);
    string GetDefaultMimeType(string extension);
    string GetOrAddRegistryKeyStringValue(string path, string keyName, Func<string> getNewValue);
}