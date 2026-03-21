using FEx.Platforms.Abstractions.Interfaces;
using Microsoft.Win32;

namespace FEx.Platforms.Extensions;

public static class RegistryServiceExtensions
{
    public static RegistryKey GetClassesRootSubKey(this IRegistryService service, string subKey) =>
        service.GetClassesRootSubKey(subKey, true);

    public static RegistryKey GetLocalMachineSubKey(this IRegistryService service, string subKey) =>
        service.GetLocalMachineSubKey(subKey, true);

    public static RegistryKey GetCurrentUserSubKey(this IRegistryService service, string subKey) =>
        service.GetCurrentUserSubKey(subKey, true);

    public static RegistryKey GetSubKey(this IRegistryService service, RegistryKey registry, string subKey) =>
        service.GetSubKey(registry, subKey, true);

    public static RegistryKey GetOrAddCurrentUserSubKey(this IRegistryService service, string subKey) =>
        service.GetOrAddCurrentUserSubKey(subKey, true);

    public static RegistryKey GetOrAddLocalMachineSubKey(this IRegistryService service, string subKey) =>
        service.GetOrAddLocalMachineSubKey(subKey, true);

    public static void SetStartup(this IRegistryService service, string appName, string executablePath, bool enable) =>
        service.SetStartup(appName, executablePath, enable, false);
}
