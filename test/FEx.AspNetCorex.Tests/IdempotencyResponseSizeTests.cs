using FEx.AspNetCorex.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.AspNetCorex.Tests;

/// <summary>
/// The defect these exist for: every keyed POST was buffered whole into an unbounded MemoryStream and stored
/// as-is, so a large response pinned unbounded memory per request and in the 24-hour cache. A response over
/// the cap must still reach the client in full, must not be stored, and the skip must be logged.
/// </summary>
// Runs the middleware, so it shares the static in-flight lock dictionary with the other middleware tests.
[Collection(IdempotencyLockCollection.Name)]
public sealed class IdempotencyResponseSizeTests
{
    private static readonly Guid Key = Guid.Parse("0b0f4c55-0ad6-4bd2-9a8a-1d1d6a2a6b01");

    [Fact]
    public async Task ResponseOverTheCap_ReachesTheClientWhole_IsNotStored_AndIsLogged()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        RecordingLogger logger = new();
        var executions = 0;
        var middleware = new IdempotencyMiddleware(
            async context =>
            {
                executions++;

                // Several writes, so the spill happens mid-body rather than on the first write.
                for (var i = 0; i < 5; i++)
                    await context.Response.WriteAsync("0123456789");
            },
            logger,
            Options.Create(new IdempotencyOptions { MaxStoredResponseBytes = 25 }));
        MemoryCacheIdempotencyStore store = new(cache);

        var first = PostContext();
        await middleware.InvokeAsync(first, store);
        var retry = PostContext();
        await middleware.InvokeAsync(retry, store);

        Body(first).ShouldBe(string.Concat(Enumerable.Repeat("0123456789", 5)));
        executions.ShouldBe(2, "an unstored response cannot be replayed, so the retry executes again");
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Information && e.Message.Contains("not stored"));
    }

    [Fact]
    public async Task ResponseAtTheCap_IsStillStoredAndReplayed()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        var executions = 0;
        var middleware = new IdempotencyMiddleware(
            context =>
            {
                executions++;

                return context.Response.WriteAsync("0123456789");
            },
            new RecordingLogger(),
            Options.Create(new IdempotencyOptions { MaxStoredResponseBytes = 10 }));
        MemoryCacheIdempotencyStore store = new(cache);

        await middleware.InvokeAsync(PostContext(), store);
        var retry = PostContext();
        await middleware.InvokeAsync(retry, store);

        executions.ShouldBe(1);
        Body(retry).ShouldBe("0123456789");
    }

    [Fact]
    public async Task DefaultCap_AppliesWithoutAnyConfiguration()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        var big = new byte[IdempotencyOptions.DefaultMaxStoredResponseBytes + 1];
        var middleware = new IdempotencyMiddleware(context => context.Response.Body.WriteAsync(big).AsTask(),
            new RecordingLogger());
        MemoryCacheIdempotencyStore store = new(cache);

        var context = PostContext();
        await middleware.InvokeAsync(context, store);

        context.Response.Body.Length.ShouldBe(big.Length);
        cache.Count.ShouldBe(0);
    }

    [Fact]
    public async Task AddIdempotency_Configure_ReachesTheMiddlewareTheHostActivates()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddIdempotency(options => options.MaxStoredResponseBytes = 4);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        RequestDelegate next = context => context.Response.WriteAsync("longer than four");

        // What UseMiddleware does: activate from the container, passing the next delegate.
        var middleware = ActivatorUtilities.CreateInstance<IdempotencyMiddleware>(provider, next);
        await middleware.InvokeAsync(PostContext(), scope.ServiceProvider.GetRequiredService<IIdempotencyStore>());

        provider.GetRequiredService<IMemoryCache>().ShouldBeOfType<MemoryCache>().Count.ShouldBe(0);
    }

    [Fact]
    public async Task MemoryStore_SizesEntriesByBody_SoASizeLimitedCacheBoundsTheTotal()
    {
        using MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 10 });
        MemoryCacheIdempotencyStore store = new(cache);
        var ct = TestContext.Current.CancellationToken;

        await store.SetAsync("a", Response(6), TimeSpan.FromHours(1), ct);
        await store.SetAsync("b", Response(6), TimeSpan.FromHours(1), ct);

        (await store.TryGetAsync("a", ct)).ShouldNotBeNull();
        (await store.TryGetAsync("b", ct)).ShouldBeNull("6 + 6 bytes exceeds the 10-byte limit");
    }

    private static IdempotentResponse Response(int size) =>
        new() { StatusCode = 200, ContentType = "application/octet-stream", Body = new byte[size] };

    private static DefaultHttpContext PostContext()
    {
        DefaultHttpContext context = new();
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers[IdempotencyMiddleware.HeaderName] = Key.ToString();
        context.User = new(new ClaimsIdentity([new("sub", "user-1")], "test"));
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static string Body(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(context.Response.Body, leaveOpen: true);

        return reader.ReadToEnd();
    }

    private sealed class RecordingLogger : ILogger<IdempotencyMiddleware>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
