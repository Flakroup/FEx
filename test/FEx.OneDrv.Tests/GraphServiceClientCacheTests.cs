using FEx.OneDrv.Abstractions;
using FEx.OneDrv.Auth;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using NSubstitute;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.OneDrv.Tests;

public sealed class GraphServiceClientCacheTests
{
    [Fact]
    public async Task GetAsync_CalledTwice_FactoriesInvokedOnce()
    {
        var auth = Substitute.For<IOneDriveAuthService>();
        var clientFactoryCalls = 0;
        var driveIdFactoryCalls = 0;
        var dummyClient = CreateDummyClient();

        using var cache = new GraphServiceClientCache(auth,
            _ =>
            {
                clientFactoryCalls++;

                return dummyClient;
            },
            (_, _) =>
            {
                driveIdFactoryCalls++;

                return Task.FromResult("drive-1");
            });

        var first = await cache.GetAsync(CancellationToken.None);
        var second = await cache.GetAsync(CancellationToken.None);

        clientFactoryCalls.ShouldBe(1);
        driveIdFactoryCalls.ShouldBe(1);
        first.Client.ShouldBeSameAs(dummyClient);
        first.DriveId.ShouldBe("drive-1");
        second.Client.ShouldBeSameAs(dummyClient);
        second.DriveId.ShouldBe("drive-1");
    }

    [Fact]
    public async Task Invalidate_AfterFirstGet_ForcesRebuildOnSecondGet()
    {
        var auth = Substitute.For<IOneDriveAuthService>();
        var clientFactoryCalls = 0;
        var dummyClient = CreateDummyClient();

        using var cache = new GraphServiceClientCache(auth,
            _ =>
            {
                clientFactoryCalls++;

                return dummyClient;
            },
            (_, _) => Task.FromResult("drive-1"));

        await cache.GetAsync(CancellationToken.None);
        cache.Invalidate();
        await cache.GetAsync(CancellationToken.None);

        clientFactoryCalls.ShouldBe(2);
    }

    [Fact]
    public async Task GetAsync_ConcurrentCalls_BuildsClientOnce()
    {
        var auth = Substitute.For<IOneDriveAuthService>();
        var clientFactoryCalls = 0;
        var dummyClient = CreateDummyClient();

        using var cache = new GraphServiceClientCache(auth,
            _ =>
            {
                Interlocked.Increment(ref clientFactoryCalls);

                return dummyClient;
            },
            async (_, ct) =>
            {
                await Task.Delay(20, ct);

                return "drive-1";
            });

        var tasks = new Task[8];

        for (var i = 0; i < tasks.Length; i++)
            tasks[i] = cache.GetAsync(CancellationToken.None);

        await Task.WhenAll(tasks);

        clientFactoryCalls.ShouldBe(1);
    }

    [Fact]
    public void Ctor_ThrowsOnNullAuth()
    {
        Should.Throw<ArgumentNullException>(() => new GraphServiceClientCache(null!));
    }

    private static GraphServiceClient CreateDummyClient()
    {
        var tokenProvider = new DelegatingAccessTokenProvider(_ => Task.FromResult("dummy"));

        return new(new BaseBearerTokenAuthenticationProvider(tokenProvider));
    }
}