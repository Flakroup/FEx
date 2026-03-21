using FEx.AppSettings.Abstractions.Interfaces;

namespace FEx.AppSettings.Extensions;

public static class ConfigurationServiceExtensions
{
    public static void Build(this IConfigurationService service) =>
        service.Build(null);

    public static bool? GetBoolSetting(this IConfigurationService service, string key) =>
        service.GetBoolSetting(key, null);
}
