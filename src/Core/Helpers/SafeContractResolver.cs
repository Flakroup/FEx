using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

namespace FEx.Core.Helpers;

public class SafeContractResolver : DefaultContractResolver
{
    private const string BindableObjectName = "BindableObject";

    private static SafeContractResolver _singleton;
    public static SafeContractResolver Singleton => _singleton ??= new();

    private SafeContractResolver()
    {
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, memberSerialization);

        if (IsSubclassOfBindableObject(prop.PropertyType))
            prop.Ignored = true;

        return prop;
    }

    private static bool IsSubclassOfBindableObject(Type type) =>
        type is not null && (type.Name == BindableObjectName || IsSubclassOfBindableObject(type.BaseType));
}