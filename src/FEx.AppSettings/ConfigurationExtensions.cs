using FEx.Extensions;
using FEx.Extensions.Collections;
using FEx.Extensions.Collections.Lists;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace FEx.AppSettings;

public static class ConfigurationExtensions
{
    public static T VerifyAppSettings<T>(this T appSettings, params string[] keys)
    {
        if (appSettings is null)
            throw new NullReferenceException($"{nameof(appSettings)} cannot be null");

        IDictionary<string, object> properties = appSettings.AsDictionary();

        if (properties.IsNullOrEmptyCollection())
            throw new ArgumentNullException($"{nameof(appSettings)} has no settings");

        string[] missingProps;

        if (keys.IsNotNullOrEmptyList())
        {
            missingProps = keys.Where(x => !properties.ContainsKey(x)).ToArray();

            if (missingProps.IsNotNullOrEmptyList())
                throw new ArgumentNullException(
                    $"{string.Join(", ", missingProps)} {(missingProps.Length == 1 ? "has" : "have")} no settings");
        }

        missingProps = properties
            .Where(p => (keys.IsNullOrEmptyList() || keys.Contains(p.Key)) && p.Value.ReferenceIsNull())
            .Select(x => x.Key)
            .ToArray();

        return missingProps.IsNotNullOrEmptyList()
            ? throw new ArgumentNullException(
                $"{string.Join(", ", missingProps)} {(missingProps.Length == 1 ? "has" : "have")} no settings")
            : appSettings;
    }

    public static TConf GetBindedConfiguration<TConf>(string sectionKey = null,
                                                      string basePath = null,
                                                      string settingsFilePath = "appsettings.json")
    {
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(basePath ?? Directory.GetCurrentDirectory()).AddJsonFile(settingsFilePath, false);

        IConfigurationRoot configuration = builder.Build();

        TConf appConfiguration = Activator.CreateInstance<TConf>();

        if (sectionKey is not null)
            configuration.GetSection(sectionKey).Bind(appConfiguration);
        else
            configuration.Bind(appConfiguration);

        return appConfiguration;
    }

    public static void BindJsonNet(this IConfiguration config, object instance, Func<string, string> jsonFunc = null)
    {
        ExpandoObject obj = BindToExpandoObject(config);

        string jsonText = JsonConvert.SerializeObject(obj);
        if (jsonFunc is not null)
            jsonText = jsonFunc(jsonText);

        JsonConvert.PopulateObject(jsonText, instance);
    }

    private static ExpandoObject BindToExpandoObject(IConfiguration config)
    {
        var result = new ExpandoObject();

        // retrieve all keys from your settings
        IEnumerable<KeyValuePair<string, string>> configs = config.AsEnumerable();
        foreach (KeyValuePair<string, string> kvp in configs)
        {
            IDictionary<string, object> parent = result;
            string[] path = kvp.Key.Split(':');

            // create or retrieve the hierarchy (keep last path item for later)
            int i;
            for (i = 0; i < path.Length - 1; i++)
            {
                if (!parent.ContainsKey(path[i]))
                    parent.Add(path[i], new ExpandoObject());

                parent = (IDictionary<string, object>)parent[path[i]];
            }

            if (kvp.Value is not null)
                parent.Add(path[i], kvp.Value);

            // add the value to the parent
            // note: in case of an array, key will be an integer and will be dealt with later
        }

        // at this stage, all arrays are seen as dictionaries with integer keys
        ReplaceWithArray(null, null, result);

        return result;
    }

    private static void ReplaceWithArray(ExpandoObject parent, string key, ExpandoObject input)
    {
        if (input is not null)
        {
            IDictionary<string, object> dict = input;
            string[] keys = dict.Keys.ToArray();

            // it's an array if all keys are integers
            if (keys.All(k => int.TryParse(k, out int dummy)))
            {
                var array = new object[keys.Length];
                foreach (KeyValuePair<string, object> kvp in dict)
                    array[int.Parse(kvp.Key)] = kvp.Value;

                IDictionary<string, object> parentDict = parent;
                parentDict.Remove(key);
                parentDict.Add(key, array);
            }
            else
            {
                foreach (string childKey in dict.Keys.ToList())
                    ReplaceWithArray(input, childKey, dict[childKey] as ExpandoObject);
            }
        }
    }
}