using FEx.AspNetCorex.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.AspNetCorex.Tests;

/// <summary>
/// The default store, and the registration that hands it over. Its whole documented limitation is that it
/// lives in the process - so the registration must let a host replace it without the default quietly winning.
/// </summary>
public sealed class MemoryCacheIdempotencyStoreTests
{
    [Fact]
    public async Task StoredResponse_ComesBackIntact()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        MemoryCacheIdempotencyStore store = new(cache);

        var ct = TestContext.Current.CancellationToken;

        await store.SetAsync(
            "k",
            new IdempotentResponse { StatusCode = 201, ContentType = "text/plain", Body = [1, 2, 3] },
            TimeSpan.FromHours(1),
            ct);

        var stored = await store.TryGetAsync("k", ct);

        stored.ShouldNotBeNull();
        stored.StatusCode.ShouldBe(201);
        stored.ContentType.ShouldBe("text/plain");
        stored.Body.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task UnknownKey_ReadsAsNothingStored()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        MemoryCacheIdempotencyStore store = new(cache);

        (await store.TryGetAsync("never-written", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public void AddIdempotency_SuppliesTheInMemoryDefault()
    {
        ServiceCollection services = new();
        services.AddIdempotency();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IIdempotencyStore>().ShouldBeOfType<MemoryCacheIdempotencyStore>();
    }

    /// <summary>A host that registered durable storage must keep it. <c>TryAdd</c> is what makes the order
    /// harmless - get this wrong and an app protecting payments silently falls back to a cache that forgets
    /// on restart.</summary>
    [Fact]
    public void AddIdempotency_NeverDisplacesAStoreTheHostAlreadyRegistered()
    {
        ServiceCollection services = new();
        services.AddScoped<IIdempotencyStore, DurableStub>();
        services.AddIdempotency();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IIdempotencyStore>().ShouldBeOfType<DurableStub>();
    }

    private sealed class DurableStub : IIdempotencyStore
    {
        public Task<IdempotentResponse?> TryGetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult<IdempotentResponse?>(null);

        public Task SetAsync(
            string key,
            IdempotentResponse response,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
