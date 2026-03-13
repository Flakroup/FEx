using FEx.Agnostics.Abstractions.Extensions;
using FEx.AppSettings.Abstractions.Interfaces;
using FEx.AppSettings.ConfigurationEx;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.AppSettings;

public class ConfigurationService : IConfigurationService
{
    public IConfigurationRoot Configuration { get; private set; }
    public Dictionary<string, string> AppSettings { get; private set; }

    public void Build(IEnumerable<IConfigurationSource> sources = null)
    {
        if (Configuration is null)
        {
            var builder = new ConfigurationBuilder().Add(new LegacyConfigurationProvider());

            if (sources is not null)
                foreach (var source in sources)
                    builder.Add(source);

            Configuration = builder.Build();
            AppSettings = Configuration.GetChildren().ToDictionary(x => x.Key, x => x.Value);
        }
        else
        {
            throw new InvalidOperationException("Configuration is already built");
        }
    }

    public T GetSetting<T>(string key, Func<string, T> func)
    {
        EnsureConfiguration();

        return func(AppSettings[key]);
    }

    public bool? GetBoolSetting(string key, bool? defaultValue = null)
    {
        EnsureConfiguration();

        return AppSettings.IsNotNullOrEmptyCollection() && AppSettings.TryGetValue(key, out var setting)
            ? StringToBool(setting)
            : defaultValue;
    }

    private static bool? StringToBool(string appSetting)
    {
        if (appSetting.IsEqual("true"))
            return true;

        return appSetting.IsEqual("false")
            ? false
            : throw new InvalidOperationException($"No conversion to bool from {appSetting} string was provided");
    }

    private void EnsureConfiguration()
    {
        if (Configuration is null)
            Build();
    }
}