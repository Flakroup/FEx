using Shouldly;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Offline.Tests;

/// <summary>
/// Read cache: fresh data is stored and stamped; a transport failure serves the stamped snapshot;
/// a server-produced error is NEVER masked by stale data.
/// </summary>
public sealed class OfflineReadCacheTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 15, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SuccessfulFetch_ReturnsFresh_AndStamps()
    {
        OfflineReadCache cache = new(new InMemoryKeyValueStore(), new FixedTime(T0));

        var result = await cache.GetAsync("dash", () => Task.FromResult(new Dashboard(3)));

        result.FromCache.ShouldBeFalse();
        result.Value.Arrears.ShouldBe(3);
        result.AsOfUtc.ShouldBe(T0);
    }

    [Fact]
    public async Task TransportFailure_ServesTheLastSnapshot_WithItsOriginalTimestamp()
    {
        FixedTime time = new(T0);
        OfflineReadCache cache = new(new InMemoryKeyValueStore(), time);
        await cache.GetAsync("dash", () => Task.FromResult(new Dashboard(3)));
        time.Advance(TimeSpan.FromHours(2));

        var offline = await cache.GetAsync<Dashboard>("dash", () => throw new HttpRequestException("offline"));

        offline.FromCache.ShouldBeTrue();
        offline.Value.Arrears.ShouldBe(3);
        offline.AsOfUtc.ShouldBe(T0); // the age of the DATA, not of the failed attempt
    }

    [Fact]
    public async Task TransportFailure_WithoutASnapshot_Propagates()
    {
        OfflineReadCache cache = new(new InMemoryKeyValueStore(), new FixedTime(T0));

        await Should.ThrowAsync<HttpRequestException>(() =>
            cache.GetAsync<Dashboard>("dash", () => throw new HttpRequestException("offline")));
    }

    [Fact]
    public async Task ServerProducedError_Propagates_EvenWithASnapshotAvailable()
    {
        OfflineReadCache cache = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await cache.GetAsync("dash", () => Task.FromResult(new Dashboard(3)));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            cache.GetAsync<Dashboard>("dash",
                () => throw new InvalidOperationException("HTTP 500 mapped by the client")));
    }

    [Fact]
    public async Task NewerFetch_ReplacesTheSnapshot()
    {
        FixedTime time = new(T0);
        OfflineReadCache cache = new(new InMemoryKeyValueStore(), time);
        await cache.GetAsync("dash", () => Task.FromResult(new Dashboard(3)));
        time.Advance(TimeSpan.FromMinutes(30));
        await cache.GetAsync("dash", () => Task.FromResult(new Dashboard(7)));

        var offline = await cache.GetAsync<Dashboard>("dash", () => throw new HttpRequestException("offline"));

        offline.Value.Arrears.ShouldBe(7);
        offline.AsOfUtc.ShouldBe(T0 + TimeSpan.FromMinutes(30));
    }

    private sealed record Dashboard(int Arrears);
}