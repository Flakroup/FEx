using FEx.Json.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StrongInject;
using System;
using System.Linq;
using Xunit;

namespace FEx.Json.Tests;

[RegisterModule(typeof(FExJsonModule))]
public sealed partial class NewtonsoftJsonTestContainer : IFExJsonContainer;

public sealed class FExJsonModuleTests
{
    private sealed class TestModule : FExJsonModule
    {
        private readonly IFExJsonContainer _container;

        public TestModule(IFExJsonContainer container)
        {
            _container = container;
        }

        protected override IFExJsonContainer GetModule() => _container;
    }

    [Fact]
    public void Module_RegistersTheNewtonsoftSerializer()
    {
        using var container = new NewtonsoftJsonTestContainer();

        using var serializer = ((IContainer<IFExJsonSerializer>)container).Resolve();

        serializer.Value.ShouldBeOfType<FExNewtonsoftJsonSerializer>().Serialize(new { Name = "Ada" })
            .ShouldBe("{\"Name\":\"Ada\"}");
    }

    [Fact]
    public void Module_RegisterServices_AddsTheSerializerAsSingleton()
    {
        using var container = new NewtonsoftJsonTestContainer();
        var services = new ServiceCollection();

        new TestModule(container).RegisterServices(services);

        services.Single(static d => d.ServiceType == typeof(IFExJsonSerializer)).Lifetime
            .ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void Serializer_NullSettings_Throw()
    {
        Should.Throw<ArgumentNullException>(() => new FExNewtonsoftJsonSerializer(null!));
    }
}
