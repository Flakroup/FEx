using Microsoft.Extensions.Configuration;
using System.Configuration;
using ConfigurationManager = System.Configuration.ConfigurationManager;

namespace FEx.AppSettings.ConfigurationEx;

public class LegacyConfigurationProvider : ConfigurationProvider, IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) => this;

    public override void Load()
    {
        foreach (ConnectionStringSettings connectionString in ConfigurationManager.ConnectionStrings)
            Data.Add($"ConnectionStrings:{connectionString.Name}", connectionString.ConnectionString);

        foreach (string settingKey in ConfigurationManager.AppSettings.AllKeys)
            Data.Add(settingKey, ConfigurationManager.AppSettings[settingKey]);
    }
}