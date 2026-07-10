using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FEx.Core.Abstractions.Helpers;

public static class ReflectionHelper
{
    public static object? GetPropertyValue(this object obj, string propertyName)
    {
        obj.Guard(nameof(obj));

        var objType = obj.GetType();
        var propInfo = GetPropertyInfo(objType, propertyName);

        return propInfo is null
            ? throw new ArgumentOutOfRangeException(nameof(propertyName),
                $"Couldn't find property {propertyName} in type {objType.FullName}")
            : propInfo.GetValue(obj, null);
    }

    public static void SetPropertyValue(this object obj, string propertyName, object val)
    {
        if (obj is not null)
        {
            var objType = obj.GetType();
            var propInfo = GetPropertyInfo(objType, propertyName);

            if (propInfo is not null)
                propInfo.SetValue(obj, val, null);
            else
                throw new ArgumentOutOfRangeException(nameof(propertyName),
                    $"Couldn't find property {propertyName} in type {objType.FullName}");
        }
        else
        {
            throw new ArgumentNullException(nameof(obj));
        }
    }

    public static object? GetFieldValue(this object obj, string fieldName)
    {
        obj.Guard(nameof(obj));

        var objType = obj.GetType();
        var propInfo = GetFieldInfo(objType, fieldName);

        return propInfo is null
            ? throw new ArgumentOutOfRangeException(nameof(fieldName),
                $"Couldn't find field {fieldName} in type {objType.FullName}")
            : propInfo.GetValue(obj);
    }

    public static bool HasResource(this Assembly assembly,
                                   string resourceName,
                                   bool throwIfMissing = true,
                                   bool addNamespace = false)
    {
        var names = assembly.GetManifestResourceNames();

        if (addNamespace)
            resourceName = $"{assembly.GetName().Name}.{resourceName}";

        if (names.Contains(resourceName))
            return true;

        return throwIfMissing
            ? throw new($"There is no resource named {resourceName} in {assembly.FullName}")
            : false;
    }

    public static string? ReadEmbeddedFile(this Assembly assembly,
                                          string resourceName,
                                          bool throwIfMissing = true,
                                          bool addNamespace = false)
    {
        if (addNamespace)
            resourceName = $"{assembly.GetName().Name}.{resourceName}";

        if (!throwIfMissing
            || assembly.HasResource(resourceName))
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream is not null)
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
        }

        return null;
    }

    public static string GetEntryAssemblyAttribute<T>(this Assembly assembly, Func<T, string> func)
        where T : Attribute =>
        // FindInEnumerable is [MaybeNull]; the ! preserves the original behavior of passing the
        // (possibly-null) attribute straight to func rather than short-circuiting.
        func((T)assembly.GetCustomAttributes(typeof(T), false).FindInEnumerable()!);

    public static T ToObject<T>(this IDictionary<string, object> source) where T : class, new()
    {
        var someObject = new T();
        var someObjectType = someObject.GetType();

        foreach (var item in source)
            // Property is assumed present for the given key; a missing key throws (NRE) as before.
            someObjectType.GetProperty(item.Key)!.SetValue(someObject, item.Value, null);

        return someObject;
    }

    public static IDictionary<string, object?> AsDictionary(this object source,
                                                           BindingFlags bindingAttr =
                                                               BindingFlags.DeclaredOnly
                                                               | BindingFlags.Public
                                                               | BindingFlags.Instance) =>
        source.GetType()
            .GetProperties(bindingAttr)
            // Cast required: on down-level TFMs GetValue returns oblivious 'object', which would
            // infer Dictionary<string, object> and break the IDictionary<string, object?> contract.
            // ReSharper disable once RedundantCast
            .ToDictionary(propInfo => propInfo.Name, propInfo => (object?)propInfo.GetValue(source, null));

    private static PropertyInfo? GetPropertyInfo(Type type, string propertyName)
    {
        PropertyInfo? propInfo;
        // 'var' would infer non-null Type and break the 'currentType = currentType.BaseType' (Type?) reassignment.
        // ReSharper disable once SuggestVarOrType_SimpleTypes
        Type? currentType = type;

        do
        {
            propInfo = currentType.GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            currentType = currentType.BaseType;
        } while (propInfo is null
                 && currentType is not null);

        return propInfo;
    }

    private static FieldInfo? GetFieldInfo(Type type, string fieldName)
    {
        FieldInfo? fieldInfo;
        // 'var' would infer non-null Type and break the 'currentType = currentType.BaseType' (Type?) reassignment.
        // ReSharper disable once SuggestVarOrType_SimpleTypes
        Type? currentType = type;

        do
        {
            fieldInfo = currentType.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            currentType = currentType.BaseType;
        } while (fieldInfo is null
                 && currentType is not null);

        return fieldInfo;
    }
}