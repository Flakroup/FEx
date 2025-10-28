using System;
using System.Configuration;
using System.Linq;
using System.Reflection;

namespace FEx.AppSettings.Extensions;

public static class ConfigurationManagerExtensions
{
    public static void MergeAppSettings()
    {
        var appConfig =
            ConfigurationManager.OpenExeConfiguration(new Uri(Assembly.GetExecutingAssembly().Location).AbsolutePath);

        try
        {
            foreach (var setting in appConfig.AppSettings.Settings.AllKeys
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
            var readonlyField =
                typeof(ConfigurationElementCollection).GetField("bReadOnly",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            readonlyField?.SetValue(ConfigurationManager.ConnectionStrings, false);

            var baseAddMethod = typeof(ConfigurationElementCollection).GetMethod("BaseAdd",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                [typeof(ConfigurationElement)],
                null);

            ConnectionStringSettings[] connStrs =
                [.. ConfigurationManager.ConnectionStrings.OfType<ConnectionStringSettings>()];

            foreach (var connStr in appConfig.ConnectionStrings.ConnectionStrings.OfType<ConnectionStringSettings>()
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