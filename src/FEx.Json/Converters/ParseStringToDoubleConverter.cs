using FEx.Agnostics.Abstractions.Extensions.Numericals;
using Newtonsoft.Json;
using System;

namespace FEx.Json.Converters;

public class ParseStringToDoubleConverter : JsonConverter
{
    public static ParseStringToDoubleConverter Singleton { get; } = new();

    public override bool CanConvert(Type t) => t == typeof(double) || t == typeof(double?);

    public override object? ReadJson(JsonReader reader, Type t, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            // Returning null for a non-nullable double made Newtonsoft unbox it (NullReferenceException) or store 0.
            if (t == typeof(double?))
                return null;

            throw new JsonSerializationException("Cannot unmarshal null to type double");
        }

        var value = serializer.Deserialize<string>(reader);

        try
        {
            // Token is not Null (checked above), so the deserialized value is a non-null string.
            return value!.ToDouble();
        }
        catch (Exception ex)
        {
            // ToDouble throws a bare Exception; a Newtonsoft exception is reported like any unbindable payload.
            throw new JsonSerializationException("Cannot unmarshal type double", ex);
        }
    }

    public override void WriteJson(JsonWriter writer, object? untypedValue, JsonSerializer serializer)
    {
        if (untypedValue is null)
        {
            serializer.Serialize(writer, null);

            return;
        }

        var value = (double)untypedValue;
        serializer.Serialize(writer, value.ToString());
    }
}