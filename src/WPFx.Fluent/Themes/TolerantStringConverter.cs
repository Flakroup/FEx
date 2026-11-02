using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FEx.WPFx.Fluent.Themes;

/// <summary>
/// Reads a string and nothing else as itself: a number, boolean, null, object or array becomes an empty string
/// instead of throwing. A settings loader that turns any exception into an empty configuration would otherwise lose
/// the whole file over one hand-edited <c>"Theme": 2</c>. Put it on the raw theme setting
/// (<c>[JsonConverter(typeof(TolerantStringConverter))]</c>) and read it through <see cref="ThemeChoice.ParsePreference"/>.
/// </summary>
public sealed class TolerantStringConverter : JsonConverter<string>
{
    /// <summary>System.Text.Json skips the converter for a null token unless told otherwise; it then reads as an empty string.</summary>
    public override bool HandleNull => true;

    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return reader.GetString() ?? string.Empty;

        _ = reader.TrySkip(); // Skip() throws on a non-final block of a stream; TrySkip waits for the rest
        return string.Empty;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
