using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Platforms.Abstractions.Interfaces;
using FEx.Platforms.Extensions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CA1416

namespace FEx.Platforms;

public class RegistryService : IRegistryService
{
    private const string Release = "Release";
    private static RegistryService _instance;

    public static RegistryService Instance => _instance ??= new();

    private static bool Is64BitOperatingSystem => PlatformInfoProvider.Is64BitOperatingSystem;

    public List<RegistryKey> GetInstalledApplications()
    {
        const string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        const string registry64Key = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
        var keys = new List<RegistryKey>();

        using (var lm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
        {
            using var key = lm.OpenSubKey(registryKey);

            if (key is not null)
                keys.AddRange([.. key.GetSubKeyNames().Select(key.OpenSubKey)]);
        }

        using (var lm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
        {
            using var key = lm.OpenSubKey(registry64Key);

            if (key is not null)
                keys.AddRange([.. key.GetSubKeyNames().Select(key.OpenSubKey)]);
        }

        return keys;
    }

    public List<Version> GetVersionFromRegistry()
    {
        var versions = new List<Version>();

        // Opens the registry key for the .NET Framework entry.
        using var ndpKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "")
            .OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\");

        // As an alternative, if you know the computers you will query are running .NET Framework 4.5 
        // or later, you can use:
        // using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, 
        // RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
        foreach (var versionKeyName in ndpKey!.GetSubKeyNames())
        {
            if (versionKeyName.StartsWith("v"))
            {
                using var versionKey = ndpKey.OpenSubKey(versionKeyName);
                var name = (string)versionKey!.GetValue("Version", "");
                var sp = versionKey.GetValue("SP", "").ToString();
                var install = versionKey.GetValue("Install", "").ToString();

                if (name.Length != 0)
                {
                    if (install!.Length != 0) //no install info, must be later.
                        versions.Add(new(name));
                    else if (sp!.Length != 0
                             && install == "1")
                        versions.Add(new(name));
                    // versions.Add($"{versionKeyName}  {name}  SP{sp}");
                }
                else
                {
                    foreach (var subKeyName in versionKey.GetSubKeyNames())
                    {
                        if (versionKeyName != "v4"
                            && subKeyName != "Full")
                        {
                            using var subKey = versionKey.OpenSubKey(subKeyName);
                            name = (string)(subKey?.GetValue("Version", "") ?? "");

                            if (name.Length != 0)
                                sp = subKey?.GetValue("SP", "").ToString() ?? "";

                            install = subKey?.GetValue("Install", "").ToString() ?? "";

                            if (install.Length == 0) //no install info, must be later.
                                versions.Add(new(name)); //}  {name}");
                            else if (sp!.Length != 0
                                     && install == "1")
                                versions.Add(new(name)); // }  {name}  SP{sp}");
                            else if (install == "1")
                                versions.Add(new(name)); //}  {name}");
                        }
                    }
                }
            }
        }

        return versions;
    }

    public List<Version> Get45PlusFromRegistry()
    {
        const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

        using var ndpKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "").OpenSubKey(subkey);

        return ndpKey?.GetValue(Release) is not null
            ? CheckFor45PlusVersion((int)ndpKey.GetValue(Release)!)
            : null;
    }

    public RegistryKey GetClassesRootSubKey(string subKey, bool writable) =>
        RunClassesRootFunc(lm => GetSubKey(lm, subKey, writable));

    public RegistryKey GetLocalMachineSubKey(string subKey, bool writable) =>
        RunLocalMachineFunc(lm => GetSubKey(lm, subKey, writable));

    public RegistryKey GetCurrentUserSubKey(string subKey, bool writable) =>
        RunCurrentUserFunc(cu => GetSubKey(cu, subKey, writable));

    public RegistryKey GetSubKey(RegistryKey registry, string subKey, bool writable) =>
        registry.OpenSubKey(subKey, writable);

    public RegistryKey GetOrAddCurrentUserSubKey(string subKey, bool writable) =>
        RunCurrentUserFunc(cu => GetOrAddSubKey(cu, subKey, writable));

    public RegistryKey GetOrAddLocalMachineSubKey(string subKey, bool writable) =>
        RunLocalMachineFunc(lm => GetOrAddSubKey(lm, subKey, writable));

    public void SetStartup(string appName, string executablePath, bool enable, bool global)
    {
        const string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

        using var startupKey = global
            ? GetOrAddLocalMachineSubKey(runKey, true)
            : GetOrAddCurrentUserSubKey(runKey, true);

        if (enable)
            startupKey.SetValue(appName, executablePath);
        else
            startupKey.DeleteValue(appName, false);
    }

    public string GetStandardBrowserPath()
    {
        var browserPath = string.Empty;
        RegistryKey browserKey = null;

        try
        {
            //Read default browser path from Win XP registry key
            browserKey = Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command", false);

            //If browser path wasn't found, try Win Vista (and newer) registry key
            browserKey ??=
                Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http",
                    false);

            //If browser path was found, clean it
            if (browserKey is not null)
            {
                //Remove quotation marks
                browserPath = (browserKey.GetValue(null) as string)?.ToLower().Replace("\"", "");

                //Cut off optional parameters
                if (browserPath?.EndsWith("exe") == false)
                    browserPath =
                        browserPath.Substring(0, browserPath.LastIndexOf(".exe", StringComparison.Ordinal) + 4);

                //Close registry key
                browserKey.Close();
            }
        }
        catch
        {
            //Return empty string, if no path was found
            return string.Empty;
        }
        finally
        {
            browserKey?.Dispose();
        }

        //Return default browsers path
        return browserPath;
    }

    public string GetDefaultExtension(string mimeType)
    {
        using var key = GetClassesRootSubKey($@"MIME\Database\Content Type\{mimeType}", false);
        const string name = "Extension";

        return key?.GetValue(name, null)?.ToString();
    }

    public string GetDefaultMimeType(string extension)
    {
        using var key = GetClassesRootSubKey(@"MIME\Database\Content Type", false);

        return key?.GetSubKeyNames().FirstOrDefault(subKey => GetDefaultExtension(subKey) == extension);
    }

    public string GetDefaultExtension(MediaTypes mediaType) => GetDefaultExtension(mediaType.GetEnumValueDescription());

    public string GetOrAddRegistryKeyStringValue(string path, string keyName, Func<string> getNewValue)
    {
        using var reg = GetOrAddCurrentUserSubKey(path, true);

        if (PlatformInfoProvider.IsWindows
            && !reg.GetValueNames().Contains(keyName))
            reg.SetValue(keyName, getNewValue(), RegistryValueKind.String);

        return reg.GetKeyValue<string>(keyName);
    }

    // Checking the version using >= will enable forward compatibility.
    private static List<Version> CheckFor45PlusVersion(int releaseKey)
    {
        var versions = new List<Version>();

        if (releaseKey >= 461808)
            versions.Add(new("4.7.2")); //or later;

        if (releaseKey >= 461308)
            versions.Add(new("4.7.1"));

        if (releaseKey >= 460798)
            versions.Add(new("4.7"));

        if (releaseKey >= 394802)
            versions.Add(new("4.6.2"));

        if (releaseKey >= 394254)
            versions.Add(new("4.6.1"));

        if (releaseKey >= 393295)
            versions.Add(new("4.6"));

        if (releaseKey >= 379893)
            versions.Add(new("4.5.2"));

        if (releaseKey >= 378675)
            versions.Add(new("4.5.1"));

        if (releaseKey >= 378389)
            versions.Add(new("4.5"));

        return versions;
    }

    private static T RunClassesRootFunc<T>(Func<RegistryKey, T> func)
    {
        using var lm = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot,
            Is64BitOperatingSystem
                ? RegistryView.Registry64
                : RegistryView.Registry32);

        return func(lm);
    }

    private static T RunLocalMachineFunc<T>(Func<RegistryKey, T> func)
    {
        using var lm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
            Is64BitOperatingSystem
                ? RegistryView.Registry64
                : RegistryView.Registry32);

        return func(lm);
    }

    private static T RunCurrentUserFunc<T>(Func<RegistryKey, T> func)
    {
        using var cu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,
            Is64BitOperatingSystem
                ? RegistryView.Registry64
                : RegistryView.Registry32);

        return func(cu);
    }

    private RegistryKey GetOrAddSubKey(RegistryKey root, string subKey, bool writable)
    {
#pragma warning disable IDISP001
        var reg = GetSubKey(root, subKey, writable);
#pragma warning restore IDISP001

        if (reg is not null)
            return reg;

        using var _ = root.CreateSubKey(subKey);

        return GetSubKey(root, subKey, writable);
    }
}
#pragma warning restore CA1416