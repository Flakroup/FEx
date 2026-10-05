using Newtonsoft.Json;
using System;

namespace FEx.Json.Converters;

public class ParseStringToLongConverter : JsonConverter
{
    public static ParseStringToLongConverter Singleton { get; } = new();

    public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

    public override object? ReadJson(JsonReader reader, Type t, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            // Returning null for a non-nullable long made Newtonsoft unbox it (NullReferenceException) or store 0.
            if (t == typeof(long?))
                return null;

            throw new JsonSerializationException("Cannot unmarshal null to type long");
        }

        var value = serializer.Deserialize<string>(reader);

        if (long.TryParse(value, out var l))
            return l;

        // A Newtonsoft exception, so the serializer reports it like any other unbindable payload.
        throw new JsonSerializationException("Cannot unmarshal type long");
    }

    public override void WriteJson(JsonWriter writer, object? untypedValue, JsonSerializer serializer)
    {
        if (untypedValue == null)
        {
            serializer.Serialize(writer, null);

            return;
        }

        var value = (long)untypedValue;
        serializer.Serialize(writer, value.ToString());
    }
}