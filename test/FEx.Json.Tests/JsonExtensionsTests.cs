using FEx.Json.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace FEx.Json.Tests;

public sealed class JsonExtensionsTests
{
    public sealed class Sample
    {
        public string? Name { get; set; }
        public int Count { get; set; }
        public double? Ratio { get; set; }
        public Version? Version { get; set; }
    }

    [Fact]
    public void DefaultSettings_AreInstalledAsNewtonsoftDefaults()
    {
        JsonExtensions.Initialize();

        JsonConvert.DefaultSettings.ShouldNotBeNull();
        var settings = JsonExtensions.DefaultSettings!;
        settings.NullValueHandling.ShouldBe(NullValueHandling.Ignore);
        settings.DateParseHandling.ShouldBe(DateParseHandling.None);
        settings.MissingMemberHandling.ShouldBe(MissingMemberHandling.Ignore);
        settings.MetadataPropertyHandling.ShouldBe(MetadataPropertyHandling.Ignore);
    }

    [Fact]
    public void ToJson_OmitsNullProperties()
    {
        new Sample { Name = null, Count = 2 }.ToJson().ShouldBe("{\"Count\":2}");
    }

    [Fact]
    public void ToJson_SerializesVersionAsString()
    {
        new Sample { Version = new(1, 2, 3) }.ToJson().ShouldBe("{\"Count\":0,\"Version\":\"1.2.3\"}");
    }

    [Fact]
    public void ToJson_SerializesDoubleAsString()
    {
        new Sample { Ratio = 1.5 }.ToJson().ShouldContain("\"Ratio\":\"" + 1.5.ToString() + "\"");
    }

    [Fact]
    public void ToJson_Indented_UsesNewLines()
    {
        new Sample { Count = 1 }.ToJson(Formatting.Indented).ShouldContain(Environment.NewLine);
    }

    [Fact]
    public void ToJson_ExplicitSettings_OverrideDefaults()
    {
        var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include };

        new Sample().ToJson(settings).ShouldContain("\"Name\":null");
    }

    [Fact]
    public void FromJson_UnknownMembers_AreIgnored()
    {
        var sample = "{\"Name\":\"a\",\"Other\":5}".FromJson<Sample>();

        sample!.Name.ShouldBe("a");
    }

    [Fact]
    public void FromJson_StringNumber_IsParsedToDouble()
    {
        "{\"Ratio\":\"2.5\"}".FromJson<Sample>()!.Ratio.ShouldBe(2.5);
    }

    [Fact]
    public void FromJson_Version_IsParsedFromString()
    {
        "{\"Version\":\"4.5.6\"}".FromJson<Sample>()!.Version.ShouldBe(new Version(4, 5, 6));
    }

    [Fact]
    public void FromJson_DateStrings_AreNotConvertedToDates()
    {
        var token = (JObject)"{\"d\":\"2020-01-02T03:04:05Z\"}".FromJson()!;

        token["d"]!.Type.ShouldBe(JTokenType.String);
    }

    [Fact]
    public void FromJson_NullString_ReturnsFallback()
    {
        JsonExtensions.NullString.FromJson<Sample>(null, new() { Name = "fb" })!.Name.ShouldBe("fb");
    }

    [Fact]
    public void FromJson_InvalidJson_Throws()
    {
        Should.Throw<JsonException>(() => "{not json".FromJson<Sample>());
    }

    [Fact]
    public void RoundTrip_PreservesValues()
    {
        var original = new Sample { Name = "n", Count = 7, Version = new(1, 0) };

        var copy = original.ToJson().FromJson<Sample>()!;

        copy.Name.ShouldBe("n");
        copy.Count.ShouldBe(7);
        copy.Version.ShouldBe(new Version(1, 0));
    }

    [Fact]
    public void DeserializeFromStream_Generic_ReadsObject()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"Name\":\"s\",\"Count\":3}"));

        var sample = stream.DeserializeFromStream<Sample>();

        sample!.Name.ShouldBe("s");
        sample.Count.ShouldBe(3);
    }

    [Fact]
    public void DeserializeToken_ReadsTypedObject()
    {
        JToken token = JObject.Parse("{\"Name\":\"t\"}");

        token.DeserializeToken<Sample>()!.Name.ShouldBe("t");
    }

    [Fact]
    public void ReformatJson_Indents()
    {
        "{\"a\":1}".ReformatJson().ShouldBe("{" + Environment.NewLine + "  \"a\": 1" + Environment.NewLine + "}");
    }

    [Fact]
    public void PrettyPrintJson_InvalidJson_ReturnsInputUnchanged()
    {
        "Service Unavailable".PrettyPrintJson().ShouldBe("Service Unavailable");
    }

    [Fact]
    public void PrettyPrintJson_WhitespaceOnly_ReturnsInput()
    {
        "  ".PrettyPrintJson().ShouldBe("  ");
    }

    [Fact]
    public void PrettyPrintJson_ValidJson_IsIndented()
    {
        "{\"a\":1}".PrettyPrintJson().ShouldContain(Environment.NewLine);
    }

    [Fact]
    public void TrimJsonString_UnescapesQuotedObject()
    {
        "  \"{\\\"a\\\":1}\"  ".TrimJsonString().ShouldBe("{\"a\":1}");
    }

    [Fact]
    public void TrimJsonString_PlainValue_OnlyTrimmed()
    {
        " abc ".TrimJsonString().ShouldBe("abc");
        ((string?)null).TrimJsonString().ShouldBeNull();
    }

    [Fact]
    public void SerializeToFile_ThenDeserializeFromFile_RoundTrips()
    {
        var file = new FileInfo(Path.GetTempFileName());

        try
        {
            file.SerializeToFile(new Sample { Name = "f", Count = 9 });

            var copy = file.DeserializeFromFile<Sample>()!;

            copy.Name.ShouldBe("f");
            copy.Count.ShouldBe(9);
        }
        finally
        {
            file.Delete();
        }
    }
}
