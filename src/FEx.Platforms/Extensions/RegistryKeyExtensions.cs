using FEx.Agnostics.Abstractions.Utilities;
using Microsoft.Win32;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Platforms.Extensions;

public static class RegistryKeyExtensions
{
    [return: MaybeNull]
    public static T GetKeyValue<T>(this RegistryKey reg, string keyName, [AllowNull] T fallback)
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

    [return: MaybeNull]
    public static T GetKeyValue<T>(this RegistryKey reg, string keyName) => reg.GetKeyValue<T>(keyName, default);
}
