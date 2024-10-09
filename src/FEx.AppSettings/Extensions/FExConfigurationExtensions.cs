using FEx.Common.Extensions;
using FEx.Extensions.Collections.Lists;
using FEx.Json.Extensions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace FEx.AppSettings.Extensions;

public static class FExConfigurationExtensions
{
    public static T VerifyAppSettings<T>(this T appSettings, params Expression<Func<T, object>>[] keys) where T : class
    {
        appSettings.Guard(nameof(appSettings), $"{nameof(appSettings)} cannot be null");

        var missingProps = keys.Where(key =>
            {
                try
                {
                    return key.Compile()(appSettings) is null;
                }
                catch
                {
                    return true;
                }
            })
            .Select(GetMemberPath)
            .ToList();

        return missingProps.IsNotNullOrEmptyList()
            ? throw new ArgumentNullException(
                $"{string.Join(", ", missingProps)} {(missingProps.Count == 1 ? "has" : "have")} no value")
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

    public static void BindJsonNet(this IConfigurationSection config,
                                   object instance,
                                   Func<string, string> jsonFunc = null)
    {
        string jsonText = GetSerializedConfig(config, jsonFunc);

        JsonConvert.PopulateObject(jsonText, instance);
    }

    public static T BindJsonNet<T>(this IConfigurationSection config, Func<string, string> jsonFunc = null)
        where T : new()
    {
        string jsonText = GetSerializedConfig(config, jsonFunc);

        return jsonText.FromJson<T>() ?? new T();
    }

    private static string GetMemberPath(Expression expression)
    {
        if (expression is LambdaExpression lambda)
            return GetBodyMemberPath(lambda.Body);

        return GetBodyMemberPath(expression);
    }

    private static string GetBodyMemberPath(Expression expression)
    {
        switch (expression.NodeType)
        {
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                var unaryExpr = (UnaryExpression)expression;

                return GetMemberPath(unaryExpr.Operand);

            case ExpressionType.MemberAccess:
                var memberExpr = (MemberExpression)expression;
                var path = new StringBuilder(memberExpr.Member.Name);

                while (memberExpr.Expression?.NodeType == ExpressionType.MemberAccess)
                {
                    memberExpr = (MemberExpression)memberExpr.Expression;
                    path.Insert(0, memberExpr.Member.Name + ".");
                }

                return path.ToString();

            default:
                throw new InvalidOperationException("Unsupported expression type: " + expression.NodeType);
        }
    }

    private static string GetSerializedConfig(IConfigurationSection config, Func<string, string> jsonFunc)
    {
        ExpandoObject obj = BindToExpandoObject(config);

        string jsonText = JsonConvert.SerializeObject(obj);

        if (jsonFunc is not null)
            jsonText = jsonFunc(jsonText);

        return jsonText;
    }

    private static ExpandoObject BindToExpandoObject(IConfigurationSection config)
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

        return result.Any()
            ? (ExpandoObject)((IDictionary<string, object>)result)[config.Key]
            : null;
    }

    private static void ReplaceWithArray(ExpandoObject parent, string key, ExpandoObject input)
    {
        if (input is null)
            return;

        IDictionary<string, object> dict = input;
        string[] keys = [.. dict.Keys];

        // it's an array if all keys are integers
        if (keys.All(k => int.TryParse(k, out int dummy)))
        {
            var array = new object[keys.Length];

            foreach (KeyValuePair<string, object> kvp in dict)
                array[int.Parse(kvp.Key)] = kvp.Value;

            IDictionary<string, object> parentDict = parent;
            parentDict?.Remove(key);
            parentDict?.Add(key, array);
        }
        else
        {
            foreach (string childKey in dict.Keys.ToList())
                ReplaceWithArray(input, childKey, dict[childKey] as ExpandoObject);
        }
    }
}