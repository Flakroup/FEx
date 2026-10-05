using FEx.Json.Abstractions;
using FEx.Json.Abstractions.Helpers;
using FEx.Json.SystemTextJsonx;
using FEx.Json.SystemTextJsonx.Converters;
using FEx.Json.SystemTextJsonx.Resolvers;
using FEx.Json.Tests.Contract;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StrongInject;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace FEx.Json.Tests;

[JsonSerializable(typeof(Person))]
internal sealed partial class ContractJsonContext : JsonSerializerContext;

[RegisterModule(typeof(FExSystemTextJsonModule))]
public sealed partial class SystemTextJsonTestContainer : IFExSystemTextJsonContainer;

public sealed class SystemTextJsonxTests
{
    private sealed class TestModule : FExSystemTextJsonModule
    {
        private readonly IFExSystemTextJsonContainer _container;

        public TestModule(IFExSystemTextJsonContainer container)
        {
            _container = container;
        }

        protected override IFExSystemTextJsonContainer GetModule() => _container;
    }

    private static readonly Person Sample = new() { Name = "Ada", Age = 36 };

    [Fact]
    public void SourceGeneratedContextOptions_AreAccepted()
    {
        var serializer = new FExSystemTextJsonSerializer(ContractJsonContext.Default.Options);

        serializer.Deserialize<Person>(serializer.Serialize(Sample)).ShouldNotBeNull().Name.ShouldBe("Ada");
    }

    [Fact]
    public void SourceGeneratedResolver_KeepsItsResolver_AndRejectsTypesOutsideIt()
    {
        var options = FExSystemTextJsonOptions.CreateDefault(ContractJsonContext.Default);
        var serializer = new FExSystemTextJsonSerializer(
            FExSystemTextJsonOptions.WithDIConstruction(options, new DIJsonTypeInfoModifier(new DIMeta())));

        serializer.Deserialize<Person>(serializer.Serialize(Sample)).ShouldNotBeNull().Age.ShouldBe(36);
        // LongHolder is not in the context; no reflection fallback is added behind the consumer's back.
        Should.Throw<FExJsonException>(() => serializer.Serialize(new LongHolder()))
            .InnerException.ShouldBeOfType<NotSupportedException>();
    }

    [Fact]
    public void WithDIConstruction_CopiesTheOptions()
    {
        var options = FExSystemTextJsonOptions.CreateDefault();

        var configured = FExSystemTextJsonOptions.WithDIConstruction(options, new DIJsonTypeInfoModifier(new DIMeta()));
        new FExSystemTextJsonSerializer(configured).Serialize(Sample);

        configured.ShouldNotBeSameAs(options);
        options.IsReadOnly.ShouldBeFalse();
        options.TypeInfoResolver.ShouldBeOfType<DefaultJsonTypeInfoResolver>();
        configured.DefaultIgnoreCondition.ShouldBe(JsonIgnoreCondition.WhenWritingNull);
    }

    [Fact]
    public void DefaultOptions_DoNotWriteNulls_AndMatchNamesCaseInsensitively()
    {
        var serializer = new FExSystemTextJsonSerializer(
            FExSystemTextJsonOptions.WithDIConstruction(FExSystemTextJsonOptions.CreateDefault(),
                new DIJsonTypeInfoModifier(new DIMeta())));

        serializer.Serialize(Sample).ShouldNotContain("Address");
        serializer.Deserialize<Person>("{\"name\":\"x\"}").ShouldNotBeNull().Name.ShouldBe("x");
    }

    [Fact]
    public void WithDIConstruction_OptionsWithoutResolver_Throw()
    {
        Should.Throw<ArgumentException>(() =>
            FExSystemTextJsonOptions.WithDIConstruction(new(), new DIJsonTypeInfoModifier(new DIMeta())));
    }

    // The module tests share FExSystemTextJsonModule.Options, so they run as one test.
    [Fact]
    public void Module_RegistersTheSerializer_FromTheConsumerOptions()
    {
        try
        {
            FExSystemTextJsonModule.Options = null;

            using (var unconfigured = new SystemTextJsonTestContainer())
            {
                Should.Throw<InvalidOperationException>(() =>
                {
                    using var owned = ((IContainer<IFExJsonSerializer>)unconfigured).Resolve();
                });
            }

            FExSystemTextJsonModule.Options = FExSystemTextJsonOptions.CreateDefault();

            using (var container = new SystemTextJsonTestContainer())
            {
                using var serializer = ((IContainer<IFExJsonSerializer>)container).Resolve();
                serializer.Value.ShouldBeOfType<FExSystemTextJsonSerializer>().Serialize(new LongHolder())
                    .ShouldBe("{\"Amount\":\"0\"}");
            }

            FExSystemTextJsonModule.Options = FExSystemTextJsonOptions.CreateDefault(ContractJsonContext.Default);

            using (var container = new SystemTextJsonTestContainer())
            {
                using var serializer = ((IContainer<IFExJsonSerializer>)container).Resolve();
                serializer.Value.Serialize(Sample).ShouldContain("\"Ada\"");
                Should.Throw<FExJsonException>(() => serializer.Value.Serialize(new LongHolder()));

                var services = new ServiceCollection();
                new TestModule(container).RegisterServices(services);
                services.Single(static d => d.ServiceType == typeof(IFExJsonSerializer)).Lifetime
                    .ShouldBe(ServiceLifetime.Singleton);
                services.ShouldContain(static d => d.ServiceType == typeof(JsonSerializerOptions));
            }
        }
        finally
        {
            FExSystemTextJsonModule.Options = null;
        }
    }

    [Theory]
    [InlineData("$.a.b", "1")]
    [InlineData("a['b c']", "2")]
    [InlineData("list[1]", "4")]
    public void JsonPathSelector_SelectsSupportedPaths(string path, string expected)
    {
        using var document = JsonDocument.Parse("{\"a\":{\"b\":1,\"b c\":2},\"list\":[3,4]}");

        JsonPathSelector.TrySelect(document.RootElement, path, out var element).ShouldBeTrue();
        element.GetRawText().ShouldBe(expected);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("list[5]")]
    [InlineData("a.b.c")]
    public void JsonPathSelector_NoMatch_SelectsNothing(string path)
    {
        using var document = JsonDocument.Parse("{\"a\":{\"b\":1},\"list\":[3,4]}");

        JsonPathSelector.TrySelect(document.RootElement, path, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("a.*")]
    [InlineData("$..b")]
    [InlineData("list[?(@ > 3)]")]
    public void JsonPathSelector_MultiTokenSyntax_IsNotSupported(string path)
    {
        using var document = JsonDocument.Parse("{\"a\":{\"b\":1},\"list\":[3,4]}");

        Should.Throw<NotSupportedException>(() => JsonPathSelector.TrySelect(document.RootElement, path, out _));
    }
}
