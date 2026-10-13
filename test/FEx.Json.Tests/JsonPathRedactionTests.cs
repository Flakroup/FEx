using FEx.Json.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace FEx.Json.Tests;

/// <summary>
/// <c>GetJsonPath</c> must not hand a dictionary key to a log (#303). Newtonsoft quotes a key only when it holds
/// special characters, so a plain key such as <c>password123</c> looks exactly like a property name in the raw
/// path; the redaction walks the target type's contracts instead, and fails closed where they cannot vouch.
/// </summary>
public sealed class JsonPathRedactionTests
{
    public sealed class Account
    {
        public string? Name { get; set; }

        public Dictionary<string, int>? DailyLimits { get; set; }

        public List<Account>? Children { get; set; }

        public JObject? Extra { get; set; }

        public object? Anything { get; set; }

        [JsonProperty("display.name")]
        public int DisplayName { get; set; }

        [JsonIgnore]
        public Dictionary<string, int>? Hidden { get; set; }

        [JsonConverter(typeof(VersionConverter))]
        public Version? Version { get; set; }

        [JsonProperty(ItemConverterType = typeof(VersionConverter))]
        public List<Version>? Versions { get; set; }

        public Opaque? Sealed { get; set; }

        public OpaqueDictionary? SealedMap { get; set; }

        public Thing? Thing { get; set; }

        public ThingList? Things { get; set; }

        public ThingHolder? Holder { get; set; }
    }

    public sealed class Thing
    {
        public string? Name { get; set; }
    }

    [JsonArray(ItemConverterType = typeof(ThingConverter))]
    public sealed class ThingList : List<Thing>;

    [JsonObject(ItemConverterType = typeof(ThingConverter))]
    public sealed class ThingHolder
    {
        public Thing? Thing { get; set; }
    }

    /// <summary>Reads a <see cref="Thing" /> from any JSON shape, so its member names can be data.</summary>
    public sealed class ThingConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Thing);

        public override object ReadJson(JsonReader reader,
                                        Type objectType,
                                        object? existingValue,
                                        JsonSerializer serializer) =>
            new Thing { Name = JToken.Load(reader).ToString() };

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
            throw new NotSupportedException();
    }

    [JsonConverter(typeof(VersionConverter))]
    public sealed class Opaque
    {
        public int Inner { get; set; }
    }

    [JsonConverter(typeof(VersionConverter))]
    public sealed class OpaqueDictionary : Dictionary<string, int>;

    [Fact]
    public void PlainDictionaryKey_IsReplacedWithThePlaceholder()
    {
        var exception = Should.Throw<JsonReaderException>(() =>
            JsonConvert.DeserializeObject<Dictionary<string, int>>("{\"password123\":\"x\"}"));

        exception.Path.ShouldBe("password123");
        exception.GetJsonPath(typeof(Dictionary<string, int>)).ShouldBe("[*]");
    }

    [Fact]
    public void QuotedDictionaryKey_IsReplacedWithThePlaceholder()
    {
        var exception = Should.Throw<JsonReaderException>(() =>
            JsonConvert.DeserializeObject<Dictionary<string, int>>("{\"alice@example.com\":\"x\"}"));

        exception.Path.ShouldBe("['alice@example.com']");
        exception.GetJsonPath(typeof(Dictionary<string, int>)).ShouldBe("[*]");
    }

    [Fact]
    public void ArrayIndex_Stays()
    {
        var exception = Should.Throw<JsonReaderException>(() =>
            JsonConvert.DeserializeObject<List<int>>("[1,\"x\"]"));

        exception.GetJsonPath(typeof(List<int>)).ShouldBe("[1]");
    }

    [Fact]
    public void NestedPropertiesAndIndices_StayWhileTheKeyBelowThemIsRedacted()
    {
        var exception = Should.Throw<JsonReaderException>(() =>
            JsonConvert.DeserializeObject<Account>(
                "{\"Children\":[{\"Name\":\"n\"},{\"DailyLimits\":{\"password123\":\"x\"}}]}"));

        exception.Path.ShouldBe("Children[1].DailyLimits.password123");
        exception.GetJsonPath(typeof(Account)).ShouldBe("Children[1].DailyLimits[*]");
    }

    [Fact]
    public void QuotedPropertyName_StaysQuoted()
    {
        var exception = Should.Throw<JsonReaderException>(() =>
            JsonConvert.DeserializeObject<Account>("{\"display.name\":\"x\"}"));

        exception.GetJsonPath(typeof(Account)).ShouldBe("['display.name']");
    }

    [Fact]
    public void QuotedPropertyNameBelowAnotherSegment_IsAppendedWithoutASeparator()
    {
        var exception = Should.Throw<JsonReaderException>(() =>
            JsonConvert.DeserializeObject<Account>("{\"Children\":[{\"display.name\":\"x\"}]}"));

        exception.Path.ShouldBe("Children[0]['display.name']");
        exception.GetJsonPath(typeof(Account)).ShouldBe("Children[0]['display.name']");
    }

    [Fact]
    public void ConverterFromTheSettings_RedactsTheRestOfThePath()
    {
        var settings = new JsonSerializerSettings { Converters = [new ThingConverter()] };

        Exception("Thing.Name").GetJsonPath(typeof(Account), settings).ShouldBe("Thing[*]");
        Exception("Name").GetJsonPath(typeof(Thing), settings).ShouldBe("[*]");
    }

    [Fact]
    public void ConverterFromTheSettingsThatCannotConvertTheType_LeavesTheNames()
    {
        var settings = new JsonSerializerSettings { Converters = [new VersionConverter()] };

        Exception("Thing.Name").GetJsonPath(typeof(Account), settings).ShouldBe("Thing.Name");
    }

    [Theory]
    [InlineData("Things[0].Name", "Things[0][*]")]
    [InlineData("Holder.Thing.Name", "Holder.Thing[*]")]
    public void ContainerItemConverter_RedactsTheRestOfThePath(string path, string expected)
    {
        Exception(path).GetJsonPath(typeof(Account)).ShouldBe(expected);
    }

    [Fact]
    public void JTokenMember_RedactsTheRestOfThePath()
    {
        var exception = Should.Throw<JsonReaderException>(() =>
            JsonConvert.DeserializeObject<Account>("{\"Extra\":{\"password123\":{\"pin\":}}}"));

        exception.GetJsonPath(typeof(Account)).ShouldBe("Extra[*][*]");
    }

    [Fact]
    public void ObjectMember_RedactsTheRestOfThePath()
    {
        var exception = Should.Throw<JsonReaderException>(() =>
            JsonConvert.DeserializeObject<Account>("{\"Anything\":{\"password123\":[1,}}"));

        var path = exception.GetJsonPath(typeof(Account));

        path.ShouldStartWith("Anything[*]");
        path.ShouldNotContain("password123");
    }

    [Theory]
    [InlineData("Unknown.password123", "[*][*]")]
    [InlineData("Hidden.password123", "[*][*]")]
    [InlineData("Version.password123", "Version[*]")]
    [InlineData("Versions[0].password123", "Versions[*][*]")]
    [InlineData("Sealed.Inner", "Sealed[*]")]
    [InlineData("SealedMap[0]", "SealedMap[*]")]
    [InlineData("Name[0]", "Name[*]")]
    [InlineData("[0]", "[*]")]
    [InlineData("Children.Name", "Children[*]")]
    [InlineData("Children[0]['password\\'1']", "Children[0][*]")]
    public void SegmentTheContractsCannotVouchFor_RedactsTheRestOfThePath(string path, string expected)
    {
        Exception(path).GetJsonPath(typeof(Account)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Children[", "Children[*]")]
    [InlineData("Children[x]", "Children[*]")]
    [InlineData("Children[-1]", "Children[*]")]
    [InlineData("Children[0]['unterminated", "Children[0][*]")]
    [InlineData("Children[0]['a'x", "Children[0][*]")]
    [InlineData("Children[0].", "Children[0][*]")]
    [InlineData("Children[0]..Name", "Children[0][*]")]
    public void MalformedPath_RedactsFromTheMalformedSegmentOn(string path, string expected)
    {
        Exception(path).GetJsonPath(typeof(Account)).ShouldBe(expected);
    }

    [Fact]
    public void DictionaryOfUntypedValues_RedactsBelowTheKey()
    {
        Exception("password123.pin").GetJsonPath(typeof(Dictionary<string, object>)).ShouldBe("[*][*]");
    }

    [Fact]
    public void ContractResolverIsHonoured()
    {
        // Under the default resolver `daily_limits` names no property, so the whole path would be redacted.
        var resolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() };

        Exception("children[0].daily_limits.password123")
            .GetJsonPath(typeof(Account), new JsonSerializerSettings { ContractResolver = resolver })
            .ShouldBe("children[0].daily_limits[*]");
    }

    [Fact]
    public void ThrowingContractResolver_RedactsTheWholePathInsteadOfThrowing()
    {
        var resolver = Substitute.For<IContractResolver>();
        resolver.ResolveContract(Arg.Any<Type>()).Returns(_ => throw new InvalidOperationException("boom"));

        Exception("password123")
            .GetJsonPath(typeof(Account), new JsonSerializerSettings { ContractResolver = resolver })
            .ShouldBe("[*]");
    }

    [Fact]
    public void ThrowingConverter_RedactsTheWholePathInsteadOfThrowing()
    {
        var converter = Substitute.For<JsonConverter>();
        converter.CanConvert(Arg.Any<Type>()).Returns(_ => throw new InvalidOperationException("boom"));

        Exception("Name")
            .GetJsonPath(typeof(Thing), new JsonSerializerSettings { Converters = [converter] })
            .ShouldBe("[*]");
    }

    private static JsonSerializationException Exception(string path) => new("failed", path, 1, 1, null);
}
