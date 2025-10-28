using FEx.Core.Abstractions.Utilities;
using Microsoft.Win32;

namespace FEx.Platforms.Extensions;

public static class RegistryKeyExtensions
{
    public static T GetKeyValue<T>(this RegistryKey reg, string keyName, T fallback = default)
    {
        var value = PlatformInfoProvider.IsWindows
#pragma warning disable CA1416
            ? reg.GetValue(keyName, fallback)
#pragma warning restore CA1416
            : null;

        return value is not null
            ? (T)value
            : default;
    }
}