using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FEx.Json.SystemTextJsonx.Converters;

/// <summary>
/// Port of FEx.Json's <c>ParseStringToLongConverter</c>: reads a <see cref="long" /> from a JSON string or number and
/// writes it as a string. System.Text.Json applies it to <see cref="Nullable{T}" /> of <see cref="long" /> as well.
/// </summary>
public sealed class ParseStringToLongConverter : JsonConverter<long>
{
    public static ParseStringToLongConverter Singleton { get; } = new();

    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number
            && reader.TryGetInt64(out var number))
            return number;

        if (reader.TokenType == JsonTokenType.String
            && long.TryParse(reader.GetString(), out var parsed))
            return parsed;

        throw new JsonException("Cannot unmarshal type long");
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
