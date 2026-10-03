using FEx.Json.Converters;
using FEx.Json.Resolvers;
using Newtonsoft.Json;
using Shouldly;
using System.IO;
using System;
using Xunit;

namespace FEx.Json.Tests;

public sealed class ConverterAndResolverTests
{
    public sealed class Person
    {
        public string Name { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
    }

    public sealed class Money
    {
        public long Amount { get; set; }
    }

    public sealed class PathTarget
    {
        [JsonProperty("a.b")]
        public int Deep { get; set; }

        public string? Flat { get; set; }
    }

    [Fact]
    public void RenameProperty_ChangesSerializedName()
    {
        var resolver = new PropertyRenameAndIgnoreSerializerContractResolver();
        resolver.RenameProperty(typeof(Person), nameof(Person.Name), "full_name");
        var settings = new JsonSerializerSettings { ContractResolver = resolver };

        var json = JsonConvert.SerializeObject(new Person { Name = "x" }, settings);

        json.ShouldContain("\"full_name\":\"x\"");
        json.ShouldNotContain("\"Name\"");
    }

    [Fact]
    public void IgnoreProperty_OmitsProperty()
    {
        var resolver = new PropertyRenameAndIgnoreSerializerContractResolver();
        resolver.IgnoreProperty(typeof(Person), nameof(Person.Secret));
        var settings = new JsonSerializerSettings { ContractResolver = resolver };

        var json = JsonConvert.SerializeObject(new Person { Name = "x", Secret = "s" }, settings);

        json.ShouldNotContain("Secret");
        json.ShouldContain("\"Name\":\"x\"");
    }

    [Fact]
    public void Resolver_WithoutRules_LeavesTypesUntouched()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new PropertyRenameAndIgnoreSerializerContractResolver()
        };

        JsonConvert.SerializeObject(new Person { Name = "x" }, settings).ShouldContain("\"Secret\":\"\"");
    }

    [Fact]
    public void ParseStringToLongConverter_CanConvert_OnlyLongAndNullableLong()
    {
        var converter = ParseStringToLongConverter.Singleton;

        converter.CanConvert(typeof(long)).ShouldBeTrue();
        converter.CanConvert(typeof(long?)).ShouldBeTrue();
        converter.CanConvert(typeof(int)).ShouldBeFalse();
    }

    [Fact]
    public void ParseStringToLongConverter_ReadsQuotedNumber()
    {
        using var stringReader = new StringReader("\"123\"");
        using var reader = new JsonTextReader(stringReader);
        reader.Read();

        var value = ParseStringToLongConverter.Singleton
            .ReadJson(reader, typeof(long), null, JsonSerializer.CreateDefault());

        value.ShouldBe(123L);
    }

    [Fact]
    public void ParseStringToLongConverter_WritesNumberAsString()
    {
        var settings = new JsonSerializerSettings { Converters = { ParseStringToLongConverter.Singleton } };

        JsonConvert.SerializeObject(new Money { Amount = 5 }, settings).ShouldBe("{\"Amount\":\"5\"}");
    }

    [Fact]
    public void ParseStringToLongConverter_NonNumeric_ThrowsConverterMessage()
    {
        var settings = new JsonSerializerSettings { Converters = { ParseStringToLongConverter.Singleton } };

        var ex = Should.Throw<Exception>(() => JsonConvert.DeserializeObject<Money>("{\"Amount\":\"abc\"}", settings));

        ex.Message.ShouldContain("Cannot unmarshal type long");
    }

    [Fact]
    public void JsonPathConverter_MapsNestedPathsToProperties()
    {
        var settings = new JsonSerializerSettings { Converters = { new PathTargetConverter() } };

        var result = JsonConvert.DeserializeObject<PathTarget>("{\"a\":{\"b\":42},\"Flat\":\"f\"}", settings)!;

        result.Deep.ShouldBe(42);
        result.Flat.ShouldBe("f");
    }

    private sealed class PathTargetConverter : JsonPathConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(PathTarget);
    }
}
