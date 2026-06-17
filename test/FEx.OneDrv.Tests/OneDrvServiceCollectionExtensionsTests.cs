using FEx.Agnostics.Abstractions.Interfaces;
using FEx.OneDrv.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FEx.OneDrv.Tests;

public sealed class OneDrvServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOneDrv_RegistersAllPublicServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IFExLogger>());

        var options = new OneDriveOptions
        {
            ClientId = "test-client"
        };

        services.AddOneDrv(options);
        using var provider = services.BuildServiceProvider();

        provider.GetService<IOneDriveAuthService>().ShouldNotBeNull();
        provider.GetService<IGraphServiceClientCache>().ShouldNotBeNull();
        provider.GetService<IOneDriveClient>().ShouldNotBeNull();
        provider.GetService<IOneDriveItemEnumerator>().ShouldNotBeNull();
        provider.GetService<IOneDriveThumbnailService>().ShouldNotBeNull();
        provider.GetService<OneDriveOptions>().ShouldBeSameAs(options);
    }

    [Fact]
    public void AddOneDrv_RegistersServicesAsSingletons()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IFExLogger>());

        services.AddOneDrv(new()
        {
            ClientId = "x"
        });

        using var provider = services.BuildServiceProvider();

        var client1 = provider.GetService<IOneDriveClient>();
        var client2 = provider.GetService<IOneDriveClient>();
        client1.ShouldBeSameAs(client2);
    }
}