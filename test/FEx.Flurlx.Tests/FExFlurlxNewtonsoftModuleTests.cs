using FEx.Flurlx.Newtonsoft;
using Flurl.Http.Newtonsoft;
using Newtonsoft.Json;
using Shouldly;
using StrongInject;
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace FEx.Flurlx.Tests;

public sealed class FExFlurlxNewtonsoftModuleTests
{
    [Fact]
    public void JsonSerializerFactory_ReturnsNewtonsoftSerializer()
    {
        FExFlurlxNewtonsoftModule.JsonSerializerFactory().ShouldBeOfType<NewtonsoftJsonSerializer>();
    }

    [Fact]
    public void JsonSerializerFactory_ReturnsNewInstancePerCall()
    {
        FExFlurlxNewtonsoftModule.JsonSerializerFactory()
            .ShouldNotBeSameAs(FExFlurlxNewtonsoftModule.JsonSerializerFactory());
    }

    [Fact]
    public void JsonSerializerFactory_IsRegisteredAsSingleInstanceFactory()
    {
        var method = typeof(FExFlurlxNewtonsoftModule).GetMethod(
            nameof(FExFlurlxNewtonsoftModule.JsonSerializerFactory),
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            Type.EmptyTypes)!;

        var factory = method.GetCustomAttribute<FactoryAttribute>().ShouldNotBeNull();
        factory.Scope.ShouldBe(Scope.SingleInstance);
    }

    [Fact]
    public void Serialize_UsesFExDefaultSettings_OmittingNullsAndWritingDoublesAsStrings()
    {
        var json = FExFlurlxNewtonsoftModule.JsonSerializerFactory().Serialize(new Payload { Name = "x", Note = null });

        json.ShouldBe("""{"Name":"x","Amount":"0"}""");
    }

    [Fact]
    public void Deserialize_String_IgnoresMissingMembersAndReadsNumericStrings()
    {
        var serializer = FExFlurlxNewtonsoftModule.JsonSerializerFactory();

        var result = serializer.Deserialize<Payload>("""{"Name":"n","Unknown":1,"Amount":"2.5"}""");

        result.Name.ShouldBe("n");
        result.Amount.ShouldBe(2.5);
    }

    [Fact]
    public void Deserialize_Stream_ReadsPayload()
    {
        var serializer = FExFlurlxNewtonsoftModule.JsonSerializerFactory();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("""{"Name":"s","Amount":7}"""));

        var result = serializer.Deserialize<Payload>(stream);

        result.Name.ShouldBe("s");
        result.Amount.ShouldBe(7);
    }

    [Fact]
    public void Deserialize_DoesNotParseDateStringsIntoDates()
    {
        var serializer = FExFlurlxNewtonsoftModule.JsonSerializerFactory();

        var result = serializer.Deserialize<DateHolder>("""{"Value":"2024-01-02T03:04:05Z"}""");

        result.Value.ShouldBe("2024-01-02T03:04:05Z");
    }

    [Fact]
    public void Deserialize_InvalidJson_Throws()
    {
        var serializer = FExFlurlxNewtonsoftModule.JsonSerializerFactory();

        Should.Throw<JsonException>(() => serializer.Deserialize<Payload>("{not json"));
    }

    private sealed class Payload
    {
        public string? Name { get; set; }
        public string? Note { get; set; }
        public double Amount { get; set; }
    }

    private sealed class DateHolder
    {
        public string? Value { get; set; }
    }
}
