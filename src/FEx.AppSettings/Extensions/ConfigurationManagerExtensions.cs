using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;

namespace FEx.AppSettings.Extensions;

public static class ConfigurationManagerExtensions
{
    public static void MergeAppSettings()
    {
        Configuration appConfig =
            ConfigurationManager.OpenExeConfiguration(new Uri(Assembly.GetExecutingAssembly().Location).AbsolutePath);

        try
        {
            foreach (KeyValuePair<string, string> setting in appConfig.AppSettings.Settings.AllKeys
                         .Where(x => ConfigurationManager.AppSettings.AllKeys.All(y => x != y))
                         .ToDictionary(x => x, x => appConfig.AppSettings.Settings[x].Value))
                ConfigurationManager.AppSettings.Set(setting.Key, setting.Value);
        }
        catch
        {
            //ignored
        }

        try
        {
            FieldInfo readonlyField =
                typeof(ConfigurationElementCollection).GetField("bReadOnly",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            readonlyField?.SetValue(ConfigurationManager.ConnectionStrings, false);

            MethodInfo baseAddMethod = typeof(ConfigurationElementCollection).GetMethod("BaseAdd",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                [typeof(ConfigurationElement)],
                null);

            ConnectionStringSettings[] connStrs =
                ConfigurationManager.ConnectionStrings.OfType<ConnectionStringSettings>().ToArray();

            foreach (ConnectionStringSettings connStr in appConfig.ConnectionStrings.ConnectionStrings
                         .OfType<ConnectionStringSettings>()
                         .Where(connStr => connStrs.All(x => x.Name != connStr.Name))
                         .ToArray())
                baseAddMethod?.Invoke(ConfigurationManager.ConnectionStrings, [connStr]);

            readonlyField?.SetValue(ConfigurationManager.ConnectionStrings, true);
        }
        catch
        {
            //ignored
        }
    }
}