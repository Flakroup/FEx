using FEx.Agnostics.Abstractions.Extensions.Numericals;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FEx.Json.SystemTextJsonx.Converters;

/// <summary>
/// Port of FEx.Json's <c>ParseStringToDoubleConverter</c>: reads a <see cref="double" /> from a JSON string or number
/// and writes it as a string, with the same culture handling (<c>ToDouble</c> on read, <c>ToString()</c> on write).
/// System.Text.Json applies it to <see cref="Nullable{T}" /> of <see cref="double" /> as well.
/// </summary>
public sealed class ParseStringToDoubleConverter : JsonConverter<double>
{
    public static ParseStringToDoubleConverter Singleton { get; } = new();

    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String => ParseString(reader.GetString()!),
            _ => throw new JsonException($"Cannot unmarshal {reader.TokenType} to type double")
        };

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());

    private static double ParseString(string value)
    {
        try
        {
            return value.ToDouble();
        }
        catch (Exception ex)
        {
            // ToDouble throws a bare Exception; rethrow as JsonException so it is reported like any unbindable payload.
            throw new JsonException("Cannot unmarshal type double", ex);
        }
    }
}
