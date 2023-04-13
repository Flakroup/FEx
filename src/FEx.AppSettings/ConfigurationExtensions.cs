using FEx.Extensions;
using FEx.Extensions.Collections;
using FEx.Extensions.Collections.Lists;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FEx.AppSettings;

public static class ConfigurationExtensions
{
    public static T VerifyAppSettings<T>(this T appSettings, params string[] keys)
    {
        if (appSettings == null)
            throw new NullReferenceException($"{nameof(appSettings)} cannot be null");

        IDictionary<string, object> properties = appSettings.AsDictionary();

        if (properties.IsNullOrEmptyCollection())
            throw new ArgumentNullException($"{nameof(appSettings)} has no settings");

        string[] missingProps;

        if (keys.IsNotNullOrEmptyList())
        {
            missingProps = keys.Where(x => !properties.ContainsKey(x))
                .ToArray();

            if (missingProps.IsNotNullOrEmptyList())
                throw new ArgumentNullException($"{string.Join(", ", missingProps)} {(missingProps.Length == 1 ? "has" : "have")} no settings");
        }

        missingProps = properties.Where(p => (keys.IsNullOrEmptyList() || keys.Contains(p.Key)) && p.Value.ReferenceIsNull())
            .Select(x => x.Key)
            .ToArray();

        if (missingProps.IsNotNullOrEmptyList())
            throw new ArgumentNullException($"{string.Join(", ", missingProps)} {(missingProps.Length == 1 ? "has" : "have")} no settings");

        return appSettings;
    }

    public static TConf GetBindedConfiguration<TConf>(string sectionKey = null, string basePath = null, string settingsFilePath = "appsettings.json")
    {
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(basePath ?? Directory.GetCurrentDirectory())
            .AddJsonFile(settingsFilePath, false);

        IConfigurationRoot configuration = builder.Build();

        TConf appConfiguration = Activator.CreateInstance<TConf>();

        if (sectionKey != null)
            configuration.GetSection(sectionKey)
                .Bind(appConfiguration);
        else
            configuration.Bind(appConfiguration);

        return appConfiguration;
    }
}