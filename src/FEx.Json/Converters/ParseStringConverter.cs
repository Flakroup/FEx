using FEx.Extensions.Numericals;
using Newtonsoft.Json;

namespace FEx.Json.Converters;

public class ParseStringConverter : JsonConverter
{
    public static ParseStringConverter Singleton { get; } = new();

    public override bool CanConvert(Type t)
    {
        return t == typeof(double) || t == typeof(double?);
    }

    public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var value = serializer.Deserialize<string>(reader);
        return value.FromString();
    }

    public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
    {
        if (untypedValue == null)
        {
            serializer.Serialize(writer, null);
            return;
        }

        var value = (double)untypedValue;
        serializer.Serialize(writer, value.ToString());
    }
}