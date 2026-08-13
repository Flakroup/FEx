using FEx.AspNetCorex.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FEx.AspNetCorex;

public static class IdempotencyServiceCollectionExtensions
{
    /// <summary>
    /// Registers what <see cref="IdempotencyMiddleware"/> needs, defaulting to the in-process memory cache.
    /// <c>TryAdd</c>, not <c>Add</c>: a host that registered its own <see cref="IIdempotencyStore"/> - one
    /// that survives a restart - keeps it, and calling this afterwards does not silently take it away.
    /// </summary>
    public static IServiceCollection AddIdempotency(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.TryAddScoped<IIdempotencyStore, MemoryCacheIdempotencyStore>();

        return services;
    }
}
