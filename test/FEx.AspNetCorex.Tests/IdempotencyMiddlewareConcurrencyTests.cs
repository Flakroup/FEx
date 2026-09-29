using FEx.AspNetCorex.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.AspNetCorex.Tests;

/// <summary>
/// The concurrency contract the outbox needed once a second operator made overlapping retries reachable:
/// a second request with the same key must wait for the first to finish and then replay its response,
/// never execute the handler a second time - while a different key runs untouched by that wait. Every
/// scenario here genuinely overlaps two in-flight requests with a <see cref="TaskCompletionSource"/> gate,
/// not a sleep, because timing alone cannot prove a lock actually blocked the second caller.
/// </summary>
[Collection(IdempotencyLockCollection.Name)]
public sealed class IdempotencyMiddlewareConcurrencyTests
{
    private static readonly Guid KeyA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid KeyB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // A regression that made the second caller wait forever instead of racing to a second execution
    // must fail the run, not hang it - hence the timeout on every test below that blocks on a lock.
    [Fact(Timeout = 5000)]
    public async Task ConcurrentRequests_WithSameKey_HandlerRunsOnce_AndBothReplayTheSameResponse()
    {
        RecordingStore store = new();
        var executions = 0;
        TaskCompletionSource handlerEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        var middleware = Middleware(store,
            async context =>
            {
                Interlocked.Increment(ref executions);
                handlerEntered.TrySetResult();
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
                await release.Task;
#pragma warning restore VSTHRD003
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"id\":1}");
            });

        var first = PostContext(KeyA);
        var firstTask = middleware.InvokeAsync(first);

        // Wait until the first request is genuinely inside the handler before starting the second - the
        // second must arrive while the first is still in flight, not merely after it was scheduled.
        await handlerEntered.Task;

        var second = PostContext(KeyA);
        var secondTask = middleware.InvokeAsync(second);

        // The second caller can only reach the handler by acquiring the same in-flight lock the first
        // holds; while that lock is not released, the handler cannot run a second time.
        await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        executions.ShouldBe(1);

        release.TrySetResult();
        await Task.WhenAll(firstTask, secondTask);

        executions.ShouldBe(1);
        second.Response.StatusCode.ShouldBe(StatusCodes.Status201Created);
        second.Response.ContentType.ShouldBe("application/json");
        Body(first).ShouldBe(Body(second));
        Body(second).ShouldBe("{\"id\":1}");
    }

    [Fact]
    public async Task ConcurrentRequests_WithDifferentKeys_RunInParallel_NeitherWaitsForTheOther()
    {
        RecordingStore store = new();
        TaskCompletionSource keyAEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource keyARelease = new(TaskCreationOptions.RunContinuationsAsynchronously);

        var middleware = Middleware(store,
            async context =>
            {
                if (context.Request.Headers[IdempotencyMiddleware.HeaderName] == KeyA.ToString())
                {
                    keyAEntered.TrySetResult();
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
                    await keyARelease.Task;
#pragma warning restore VSTHRD003
                }

                context.Response.StatusCode = StatusCodes.Status200OK;
            });

        var firstTask = middleware.InvokeAsync(PostContext(KeyA));
        await keyAEntered.Task;

        // A different key must not be serialized behind KeyA's still-open lock.
        var secondTask = middleware.InvokeAsync(PostContext(KeyB));

        var winner = await Task.WhenAny(secondTask, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        winner.ShouldBe(secondTask);

        keyARelease.TrySetResult();
        await firstTask;
    }

    [Fact(Timeout = 5000)]
    public async Task FirstRequest_Throws_ReleasesTheKey_SoTheRetryExecutes()
    {
        RecordingStore store = new();
        var attempt = 0;

        var middleware = Middleware(store,
            context =>
            {
                attempt++;

                if (attempt == 1)
                    throw new InvalidOperationException("simulated handler failure");

                context.Response.StatusCode = StatusCodes.Status200OK;

                return Task.CompletedTask;
            });

        var first = PostContext(KeyA);
        var originalBody = first.Response.Body;

        await Should.ThrowAsync<InvalidOperationException>(() => middleware.InvokeAsync(first));

        // The response body must be restored to the caller's own stream, not left pointing at the
        // buffer the middleware disposed - an upstream exception handler still needs to write to it.
        first.Response.Body.ShouldBeSameAs(originalBody);
        originalBody.CanWrite.ShouldBeTrue();

        var retry = PostContext(KeyA);
        retry.RequestAborted = TestContext.Current.CancellationToken;
        await middleware.InvokeAsync(retry);

        attempt.ShouldBe(2);
        retry.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact(Timeout = 5000)]
    public async Task FirstRequest_Cancelled_ReleasesTheKey_SoTheRetryExecutes()
    {
        RecordingStore store = new();
        var attempt = 0;
        using CancellationTokenSource cts = new();

        var middleware = Middleware(store,
            async context =>
            {
                attempt++;

                if (attempt == 1)
                {
                    // Simulate the client aborting while the handler is already running - not a token
                    // that was cancelled before the request even reached the lock.
                    await cts.CancelAsync();
                    context.RequestAborted.ThrowIfCancellationRequested();
                }

                context.Response.StatusCode = StatusCodes.Status200OK;
            });

        var cancelled = PostContext(KeyA);
        cancelled.RequestAborted = cts.Token;

        await Should.ThrowAsync<OperationCanceledException>(() => middleware.InvokeAsync(cancelled));

        var retry = PostContext(KeyA);
        retry.RequestAborted = TestContext.Current.CancellationToken;
        await middleware.InvokeAsync(retry);

        attempt.ShouldBe(2);
        retry.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task FirstRequest_ClientDisconnectsRightAfterSuccess_StillStoresTheResponse_SoARetryReplaysInsteadOfRerunning()
    {
        CancellationAwareStore store = new();
        var executions = 0;
        using CancellationTokenSource cts = new();

        var middleware = Middleware(store,
            async context =>
            {
                executions++;
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsync("done");

                // The client that receives this response can disconnect the instant it lands - exactly
                // the case a retry exists for - and that must not cancel recording the success.
                await cts.CancelAsync();
            });

        var first = PostContext(KeyA);
        first.RequestAborted = cts.Token;

        await middleware.InvokeAsync(first);

        var retry = PostContext(KeyA);
        retry.RequestAborted = TestContext.Current.CancellationToken;
        await middleware.InvokeAsync(retry);

        executions.ShouldBe(1);
        retry.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact(Timeout = 5000)]
    public async Task WaiterCancelled_WhileQueued_DoesNotLeakItsRefCount()
    {
        RecordingStore store = new();
        TaskCompletionSource holderEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource holderRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource waiterCts = new();
        var before = IdempotencyMiddleware.InFlightLockCount;

        var middleware = Middleware(store,
            async context =>
            {
                holderEntered.TrySetResult();
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
                await holderRelease.Task;
#pragma warning restore VSTHRD003
                context.Response.StatusCode = StatusCodes.Status200OK;
            });

        var holder = PostContext(KeyA);
        holder.RequestAborted = TestContext.Current.CancellationToken;
        var holderTask = middleware.InvokeAsync(holder);
        await holderEntered.Task;

        // The waiter is queued on the same key's semaphore, not yet holding it, when its own caller
        // gives up - it must release the ref-count it took in Enter() without ever running the handler.
        var waiter = PostContext(KeyA);
        waiter.RequestAborted = waiterCts.Token;
        var waiterTask = middleware.InvokeAsync(waiter);

        await waiterCts.CancelAsync();

#pragma warning disable VSTHRD003 // awaiting a task started earlier in the same test method is intentional
        await Should.ThrowAsync<OperationCanceledException>(() => waiterTask);
#pragma warning restore VSTHRD003

        holderRelease.TrySetResult();
        await holderTask;

        IdempotencyMiddleware.InFlightLockCount.ShouldBe(before);
    }

    [Fact]
    public async Task CompletedRequests_RemoveTheirLockEntry_SoTheDictionaryDoesNotGrowWithoutBound()
    {
        RecordingStore store = new();
        var middleware = Middleware(store, _ => Task.CompletedTask);
        var before = IdempotencyMiddleware.InFlightLockCount;

        for (var i = 0; i < 10; i++)
            await middleware.InvokeAsync(PostContext(Guid.NewGuid()));

        IdempotencyMiddleware.InFlightLockCount.ShouldBe(before);
    }

    // --- Harness ----------------------------------------------------------------------------------

    private static TestPipeline Middleware(IIdempotencyStore store, RequestDelegate next) =>
        new(new IdempotencyMiddleware(next, NullLogger<IdempotencyMiddleware>.Instance), store);

    private sealed class TestPipeline
    {
        private readonly IdempotencyMiddleware _middleware;
        private readonly IIdempotencyStore _store;

        public TestPipeline(IdempotencyMiddleware middleware, IIdempotencyStore store)
        {
            _middleware = middleware;
            _store = store;
        }

        public Task InvokeAsync(HttpContext context) => _middleware.InvokeAsync(context, _store);
    }

    private sealed class RecordingStore : IIdempotencyStore
    {
        private readonly Dictionary<string, IdempotentResponse> _entries = [];
        private readonly object _gate = new();

        public Task<IdempotentResponse?> TryGetAsync(string key, CancellationToken cancellationToken = default)
        {
            lock (_gate)
                return Task.FromResult(_entries.TryGetValue(key, out var stored) ? stored : null);
        }

        public Task SetAsync(
            string key,
            IdempotentResponse response,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
                _entries[key] = response;

            return Task.CompletedTask;
        }
    }

    /// <summary>A store that honours cancellation like a real database-backed one (e.g. EF Core passing
    /// the token to <c>SaveChangesAsync</c>) - <see cref="RecordingStore"/> ignores it, which would hide
    /// a token used past the point it should be.</summary>
    private sealed class CancellationAwareStore : IIdempotencyStore
    {
        private readonly Dictionary<string, IdempotentResponse> _entries = [];

        public Task<IdempotentResponse?> TryGetAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(_entries.TryGetValue(key, out var stored) ? stored : null);
        }

        public Task SetAsync(
            string key,
            IdempotentResponse response,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _entries[key] = response;

            return Task.CompletedTask;
        }
    }

    private static DefaultHttpContext PostContext(Guid key, string userId = "user-1")
    {
        DefaultHttpContext context = new();
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers[IdempotencyMiddleware.HeaderName] = key.ToString();
        context.User = new(new ClaimsIdentity([new("sub", userId)], "test"));
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static string Body(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(context.Response.Body, leaveOpen: true);

        return reader.ReadToEnd();
    }
}
