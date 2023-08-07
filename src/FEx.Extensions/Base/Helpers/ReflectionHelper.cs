using FEx.Extensions.Collections.Enumerables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FEx.Extensions.Base.Helpers;

public static class ReflectionHelper
{
    public static object GetPropertyValue(this object obj, string propertyName)
    {
        obj.Guard(nameof(obj));

        Type objType = obj.GetType();
        PropertyInfo propInfo = GetPropertyInfo(objType, propertyName);
        return propInfo is null
            ? throw new ArgumentOutOfRangeException(nameof(propertyName),
                $"Couldn't find property {propertyName} in type {objType.FullName}")
            : propInfo.GetValue(obj, null);
    }

    public static void SetPropertyValue(this object obj, string propertyName, object val)
    {
        if (obj is not null)
        {
            Type objType = obj.GetType();
            PropertyInfo propInfo = GetPropertyInfo(objType, propertyName);
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

    public static object GetFieldValue(this object obj, string fieldName)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        Type objType = obj.GetType();
        FieldInfo propInfo = GetFieldInfo(objType, fieldName);
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
        string[] names = assembly.GetManifestResourceNames();

        if (addNamespace)
            resourceName = $"{assembly.GetName().Name}.{resourceName}";

        if (names.Contains(resourceName))
            return true;

        return throwIfMissing
            ? throw new Exception($"There is no resource named {resourceName} in {assembly.FullName}")
            : false;
    }

    public static string ReadEmbeddedFile(this Assembly assembly,
                                          string resourceName,
                                          bool throwIfMissing = true,
                                          bool addNamespace = false)
    {
        if (addNamespace)
            resourceName = $"{assembly.GetName().Name}.{resourceName}";

        if (!throwIfMissing
            || assembly.HasResource(resourceName))
        {
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
        }

        return null;
    }

    public static string GetEntryAssemblyAttribute<T>(this Assembly assembly, Func<T, string> func)
        where T : Attribute =>
        func((T)assembly?.GetCustomAttributes(typeof(T), false)?.FindInEnumerable());

    public static T ToObject<T>(this IDictionary<string, object> source) where T : class, new()
    {
        var someObject = new T();
        Type someObjectType = someObject.GetType();

        foreach (KeyValuePair<string, object> item in source)
            someObjectType.GetProperty(item.Key).SetValue(someObject, item.Value, null);

        return someObject;
    }

    public static IDictionary<string, object> AsDictionary(this object source,
                                                           BindingFlags bindingAttr =
                                                               BindingFlags.DeclaredOnly
                                                               | BindingFlags.Public
                                                               | BindingFlags.Instance)
    {
        return source.GetType()
            .GetProperties(bindingAttr)
            .ToDictionary(propInfo => propInfo.Name, propInfo => propInfo.GetValue(source, null));
    }

    private static PropertyInfo GetPropertyInfo(Type type, string propertyName)
    {
        PropertyInfo propInfo;
        do
        {
            propInfo = type.GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            type = type.BaseType;
        } while (propInfo is null
                 && type is not null);

        return propInfo;
    }

    private static FieldInfo GetFieldInfo(Type type, string fieldName)
    {
        FieldInfo fieldInfo;
        do
        {
            fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            type = type.BaseType;
        } while (fieldInfo is null
                 && type is not null);

        return fieldInfo;
    }
}