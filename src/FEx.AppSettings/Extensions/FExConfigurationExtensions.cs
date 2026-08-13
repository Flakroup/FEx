using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
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

    public static TConf GetBindedConfiguration<TConf>(string? sectionKey, string? basePath, string settingsFilePath)
    {
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(basePath ?? Directory.GetCurrentDirectory()).AddJsonFile(settingsFilePath, false);

        var configuration = builder.Build();

        var appConfiguration = Activator.CreateInstance<TConf>();

        if (sectionKey is not null)
            configuration.GetSection(sectionKey).Bind(appConfiguration);
        else
            configuration.Bind(appConfiguration);

        return appConfiguration;
    }

    public static TConf GetBindedConfiguration<TConf>() =>
        GetBindedConfiguration<TConf>(null, null, "appsettings.json");

    public static TConf GetBindedConfiguration<TConf>(string sectionKey) =>
        GetBindedConfiguration<TConf>(sectionKey, null, "appsettings.json");

    public static TConf GetBindedConfiguration<TConf>(string sectionKey, string basePath) =>
        GetBindedConfiguration<TConf>(sectionKey, basePath, "appsettings.json");

    public static void BindJsonNet(this IConfigurationSection config, object instance, Func<string, string>? jsonFunc)
    {
        var jsonText = GetSerializedConfig(config, jsonFunc);

        JsonConvert.PopulateObject(jsonText, instance);
    }

    public static void BindJsonNet(this IConfigurationSection config, object instance) =>
        config.BindJsonNet(instance, null);

    public static T BindJsonNet<T>(this IConfigurationSection config, Func<string, string>? jsonFunc) where T : new()
    {
        var jsonText = GetSerializedConfig(config, jsonFunc);

        return jsonText.FromJson<T>() ?? new T();
    }

    public static T BindJsonNet<T>(this IConfigurationSection config) where T : new() => config.BindJsonNet<T>(null);

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

    private static string GetSerializedConfig(IConfigurationSection config, Func<string, string>? jsonFunc)
    {
        var obj = BindToExpandoObject(config);

        var jsonText = JsonConvert.SerializeObject(obj);

        if (jsonFunc is not null)
            jsonText = jsonFunc(jsonText);

        return jsonText;
    }

    private static ExpandoObject? BindToExpandoObject(IConfigurationSection config)
    {
        var result = new ExpandoObject();

        // retrieve all keys from your settings
        var configs = config.AsEnumerable();

        foreach (var kvp in configs)
        {
            IDictionary<string, object?> parent = result;
            var path = kvp.Key.Split(':');

            // create or retrieve the hierarchy (keep last path item for later)
            int i;

            for (i = 0; i < path.Length - 1; i++)
            {
                if (!parent.ContainsKey(path[i]))
                    parent.Add(path[i], new ExpandoObject());

                parent = (IDictionary<string, object?>)parent[path[i]]!;
            }

            if (kvp.Value is not null)
                parent.Add(path[i], kvp.Value);

            // add the value to the parent
            // note: in case of an array, key will be an integer and will be dealt with later
        }

        // at this stage, all arrays are seen as dictionaries with integer keys
        ReplaceWithArray(null, null, result);

        return result.Any()
            ? (ExpandoObject?)((IDictionary<string, object?>)result)[config.Key]
            : null;
    }

    private static void ReplaceWithArray(ExpandoObject? parent, string? key, ExpandoObject? input)
    {
        if (input is null)
            return;

        IDictionary<string, object?> dict = input;
        string[] keys = [.. dict.Keys];

        // it's an array if all keys are integers
        if (keys.All(k => int.TryParse(k, out var dummy)))
        {
            var array = new object?[keys.Length];

            foreach (var kvp in dict)
                array[int.Parse(kvp.Key)] = kvp.Value;

            if (parent is not null && key is not null)
            {
                IDictionary<string, object?> parentDict = parent;
                parentDict.Remove(key);
                parentDict.Add(key, array);
            }
        }
        else
        {
            foreach (var childKey in dict.Keys.ToList())
                ReplaceWithArray(input, childKey, dict[childKey] as ExpandoObject);
        }
    }
}