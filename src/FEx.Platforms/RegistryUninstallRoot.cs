using Microsoft.Win32;
using System.Collections.Generic;

#pragma warning disable CA1416

namespace FEx.Platforms;

/// <summary>
/// Owns the local-machine hive and the "Uninstall" key opened from it.
/// </summary>
internal sealed class RegistryUninstallRoot : IUninstallRoot
{
#pragma warning disable IDISP008 // The root takes ownership of both keys
    private readonly RegistryKey _hive;
    private readonly RegistryKey _key;
#pragma warning restore IDISP008

    internal RegistryUninstallRoot(RegistryKey hive, RegistryKey key)
    {
        _hive = hive;
        _key = key;
    }

    public static IUninstallRoot? Open(UninstallSource source)
    {
#pragma warning disable IDISP001, IDISP017 // Ownership of the hive passes to the returned root, or it is disposed here
        var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, source.View);
        var key = hive.OpenSubKey(source.Path);

        if (key is not null)
            return new RegistryUninstallRoot(hive, key);

        hive.Dispose();

        return null;
#pragma warning restore IDISP001, IDISP017
    }

    public IReadOnlyList<string> GetEntryNames() => _key.GetSubKeyNames();

    public IUninstallEntry? OpenEntry(string name) =>
        _key.OpenSubKey(name) is { } subKey
            ? new RegistryUninstallEntry(subKey)
            : null;

    public void Dispose()
    {
        _key.Dispose();
        _hive.Dispose();
    }
}

/// <summary>
/// Owns one opened "Uninstall" subkey.
/// </summary>
internal sealed class RegistryUninstallEntry(RegistryKey key) : IUninstallEntry
{
    public object? GetValue(string name) => key.GetValue(name);

#pragma warning disable IDISP007 // The entry takes ownership of the key it wraps
    public void Dispose() => key.Dispose();
#pragma warning restore IDISP007
}
#pragma warning restore CA1416
