using FEx.Telemetry.Sentry;
using FEx.Telemetry.Sentry.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using StrongInject;
using System;
using System.Linq;
using Xunit;

namespace FEx.Telemetry.Tests;

public sealed class FExSentryxTests
{
    [Fact]
    public void Constructor_PublishesServiceThroughSentrySrv()
    {
        var service = Substitute.For<ISentryService>();

        _ = new FExSentryx(service);

        FExSentryx.SentrySrv.ShouldBeSameAs(service);
    }

    [Fact]
    public void Constructor_WhenServiceNull_ThrowsAndKeepsPreviousService()
    {
        var service = Substitute.For<ISentryService>();
        _ = new FExSentryx(service);

        Should.Throw<ArgumentNullException>(() => new FExSentryx(null!));

        FExSentryx.SentrySrv.ShouldBeSameAs(service);
    }

    [Fact]
    public void RegisterServices_RegistersSentryServiceAndModuleAsSingletons()
    {
        var sut = new ModuleFExSentryx(Substitute.For<IFExSentryModule>());
        var services = new ServiceCollection();

        sut.RegisterServices(services);

        services.Where(static d => d.ServiceType == typeof(ISentryService) || d.ServiceType == typeof(FExSentryx))
            .Select(static d => d.Lifetime)
            .ShouldBe([ServiceLifetime.Singleton, ServiceLifetime.Singleton]);
        services.Select(static d => d.ServiceType).ShouldBe(
            [typeof(Owned<ISentryService>), typeof(ISentryService), typeof(Owned<FExSentryx>), typeof(FExSentryx)],
            ignoreOrder: true);
    }

    [Fact]
    public void RegisterServices_WhenContainerMissing_Throws()
    {
        var sut = new ModuleFExSentryx(null);

        Should.Throw<ArgumentNullException>(() => sut.RegisterServices(new ServiceCollection()));
    }

    private sealed class ModuleFExSentryx(IFExSentryModule? module) : FExSentryx(Substitute.For<ISentryService>())
    {
        protected override IFExSentryModule? GetModule() => module;
    }
}
