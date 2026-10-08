using FEx.Telemetry.Sentry;
using FEx.Telemetry.Sentry.Abstractions.Interfaces;
using FEx.Telemetry.Sentry.Services;
using FEx.Telemetry.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Linq;
using Xunit;

namespace FEx.Telemetry.Tests;

public sealed class FExSentryModuleTests
{
    [Fact]
    public void Container_ResolvesSentryServiceAsSingleInstance()
    {
        using var container = new SentryContainer();

        var first = container.GetSentryService();
        var second = container.GetSentryService();
        first.ShouldBeOfType<SentryService>();
        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void Container_ResolvesSentryxSharingTheSentryService()
    {
        using var container = new SentryContainer();

        var sentryx = container.GetSentryx();
        sentryx.ShouldBeOfType<FExSentryx>();
        FExSentryx.SentrySrv.ShouldBeOfType<SentryService>();
    }

    [Fact]
    public void Container_ResolvesAccessTokenSubjectAsSingleInstance()
    {
        using var container = new SentryContainer();

        var first = container.GetAccessTokenSubject();
        first.ShouldBeOfType<TelemetryAccessTokenSubject>();
        container.GetAccessTokenSubject().ShouldBeSameAs(first);
    }

    [Fact]
    public void AddServices_BridgesContainerServicesIntoServiceCollection()
    {
        using var container = new SentryContainer();
        var services = new ServiceCollection();

        FExSentryModule.AddServices(container, services);

        services.Select(static d => d.ServiceType).ShouldContain(typeof(ISentryService));
        services.Select(static d => d.ServiceType).ShouldContain(typeof(FExSentryx));
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ISentryService>().ShouldBeOfType<SentryService>();
        provider.GetRequiredService<FExSentryx>().ShouldNotBeNull();
    }
}
