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
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return reader.GetString() ?? string.Empty;

        reader.Skip();
        return string.Empty;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
