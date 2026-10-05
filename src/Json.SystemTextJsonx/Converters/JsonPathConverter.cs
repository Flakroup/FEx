using System;
#if NET
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FEx.Json.SystemTextJsonx.Converters;

/// <summary>
/// Port of FEx.Json's <c>JsonPathConverter</c>: each public property of <typeparamref name="T" /> with a public setter is read
/// from the JSON path in its <see cref="JsonPropertyNameAttribute" /> (or its own name), for example
/// <c>[JsonPropertyName("data.items[0].name")]</c>. Apply it with <c>[JsonConverter(typeof(JsonPathConverter&lt;T&gt;))]</c>
/// on <typeparamref name="T" />. See <see cref="JsonPathSelector" /> for the supported path syntax. Writing produces a
/// flat object keyed by those paths, as the Newtonsoft converter does by falling back to default serialization.
/// </summary>
/// <remarks>
/// The members of <typeparamref name="T" /> are read through reflection; the annotation keeps them through trimming.
/// The property values go through <see cref="JsonSerializerOptions.GetTypeInfo" />, so under Native AOT the property
/// types need metadata in the configured resolver. Unlike the Newtonsoft converter, a <see cref="JsonIgnoreAttribute" />
/// member or one with a non-public setter is not bound, as plain System.Text.Json binding would not bind it.
/// </remarks>
public sealed class JsonPathConverter<
#if NET
    // Only .NET has the trimming annotations public; down-level targets are never trimmed.
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties
                                | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
    T> : JsonConverter<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException($"Cannot unmarshal {root.ValueKind} to type {typeof(T).FullName}");

        object target = Activator.CreateInstance<T>()!;

        // Like plain System.Text.Json binding, a [JsonIgnore] member or one without a public setter is never bound, so
        // a model that guards a member against mass assignment stays guarded.
        foreach (var property in GetProperties().Where(static p => p.GetSetMethod() is not null && !IsIgnored(p)))
        {
            if (JsonPathSelector.TrySelect(root, GetPath(property), out var element)
                && element.ValueKind != JsonValueKind.Null)
                property.SetValue(target, element.Deserialize(options.GetTypeInfo(property.PropertyType)));
        }

        return (T)target;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var property in GetProperties().Where(static p => p.CanRead))
        {
            if (IsIgnored(property))
                continue;

            var propertyValue = property.GetValue(value);

            if (propertyValue is null
                && options.DefaultIgnoreCondition != JsonIgnoreCondition.Never)
                continue;

            writer.WritePropertyName(GetPath(property));
            JsonSerializer.Serialize(writer, propertyValue, options.GetTypeInfo(property.PropertyType));
        }

        writer.WriteEndObject();
    }

    private static PropertyInfo[] GetProperties() =>
        typeof(T).GetProperties().Where(static p => p.GetIndexParameters().Length == 0).ToArray();

    private static bool IsIgnored(PropertyInfo property) =>
        property.GetCustomAttribute<JsonIgnoreAttribute>(true) is { Condition: JsonIgnoreCondition.Always };

    private static string GetPath(PropertyInfo property) =>
        property.GetCustomAttribute<JsonPropertyNameAttribute>(true)?.Name ?? property.Name;
}
