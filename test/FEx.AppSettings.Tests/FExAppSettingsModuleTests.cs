using FEx.AppSettings.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StrongInject;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FEx.AppSettings.Tests;

/// <summary>The DI module: registers the configuration service as a singleton resolved through the container.</summary>
public sealed class FExAppSettingsModuleTests
{
    [Fact]
    public void RegisterServices_AddsTheConfigurationServiceAsASingleton()
    {
        ServiceCollection services = [];
        using FakeContainer container = new(new());
        TestModule module = new(container);

        module.RegisterServices(services);

        var descriptor = services.Single(x => x.ServiceType == typeof(IConfigurationService));
        descriptor.ServiceType.ShouldBe(typeof(IConfigurationService));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void ResolvingTheRegisteredService_GoesThroughTheContainer_AndIsCreatedOnce()
    {
        ConfigurationService service = new();
        using FakeContainer container = new(service);
        ServiceCollection services = [];
        new TestModule(container).RegisterServices(services);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IConfigurationService>().ShouldBeSameAs(service);
        provider.GetRequiredService<IConfigurationService>().ShouldBeSameAs(service);
        container.RunCount.ShouldBe(1);
    }

    [Fact]
    public void RegisterServices_WithoutAContainer_Throws()
    {
        ServiceCollection services = [];
        TestModule module = new(null);

        Should.Throw<ArgumentNullException>(() => module.RegisterServices(services));
    }

    [Fact]
    public async Task CompleteInitializationAsync_MarksTheModuleCompleted()
    {
        TestModule module = new(null);

        await module.CompleteInitializationAsync(new ServiceCollection());

        module.HasBeenCompleted.ShouldBeTrue();
    }

    [Fact]
    public void FExAppSettings_Initialize_MarksItInitialized_AndIsIdempotent()
    {
        FExAppSettings appSettings = new();
        appSettings.IsInitialized.ShouldBeFalse();

        appSettings.Initialize();
        appSettings.Initialize();

        appSettings.IsInitialized.ShouldBeTrue();
    }

    private sealed class TestModule : FExAppSettingsModule
    {
        private readonly IFExAppSettingsModule? _container;

        public TestModule(IFExAppSettingsModule? container) => _container = container;

        protected override IFExAppSettingsModule? GetModule() => _container;
    }

    private sealed class FakeContainer : IFExAppSettingsModule
    {
        private readonly ConfigurationService _service;

        public FakeContainer(ConfigurationService service) => _service = service;

        public int RunCount { get; private set; }

        Owned<IConfigurationService> IContainer<IConfigurationService>.Resolve()
        {
            RunCount++;

            return new(_service, null!);
        }

        Owned<FExAppSettings> IContainer<FExAppSettings>.Resolve() => new(new(), null!);

        public TResult Run<TResult, TParam>(Func<IConfigurationService, TParam, TResult> func, TParam param)
        {
            RunCount++;

            return func(_service, param);
        }

        public TResult Run<TResult, TParam>(Func<FExAppSettings, TParam, TResult> func, TParam param) =>
            func(new(), param);

        public void Dispose()
        {
        }
    }
}
