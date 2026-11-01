using FEx.WPFx.Fluent.Themes;
using Shouldly;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

public sealed class TolerantStringConverterTests
{
    private sealed class Settings
    {
        [JsonConverter(typeof(TolerantStringConverter))]
        public string Theme { get; init; } = "System";

        public string Other { get; init; } = "kept";
    }

    [Theory]
    [InlineData("\"Dark\"", "Dark")]
    [InlineData("2", "")]
    [InlineData("true", "")]
    [InlineData("{ \"a\": [1, 2] }", "")]
    [InlineData("[\"Dark\"]", "")]
    public void Read_TakesAStringAsItself_AndAnythingElseAsEmpty(string value, string expected)
    {
        var settings = JsonSerializer.Deserialize<Settings>($"{{ \"Theme\": {value}, \"Other\": \"x\" }}");

        settings!.Theme.ShouldBe(expected);
        settings.Other.ShouldBe("x", "a bad Theme value must not take the rest of the file with it");
    }

    [Fact]
    public void Write_EmitsTheStringAsIs() =>
        JsonSerializer.Serialize(new Settings { Theme = "Light" }).ShouldBe("{\"Theme\":\"Light\",\"Other\":\"kept\"}");
}
