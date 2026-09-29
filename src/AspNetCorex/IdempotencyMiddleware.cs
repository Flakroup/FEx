using FEx.AspNetCorex.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.AspNetCorex;

/// <summary>
/// Replays the stored response of a completed POST when the client retries it with the same
/// <c>Idempotency-Key</c> header, so a write executes once even when a flaky network left the client
/// unsure whether its request landed (an offline outbox relies on this). Only successful (2xx)
/// responses are stored - per authenticated user, for 24 hours. Non-POSTs, requests without a valid
/// key and anonymous requests pass through untouched.
/// </summary>
/// <remarks>
/// Where those responses live is the host's choice (<see cref="IIdempotencyStore"/>): call
/// <c>AddIdempotency()</c> for the in-memory default, or register your own store before it - an app whose
/// writes move money wants one that outlives the process. Place the middleware after authentication.
/// The in-flight lock that stops two concurrent same-key requests from both running the handler is
/// per process; a host scaled out across several instances still needs a shared lock to close that gap.
/// </remarks>
public sealed class IdempotencyMiddleware
{
    /// <summary>The request header carrying the client-generated idempotency key (a GUID).</summary>
    public const string HeaderName = "Idempotency-Key";

    private static readonly TimeSpan ResponseLifetime = TimeSpan.FromHours(24);

    // A second concurrent request with the same key must wait for the first to finish and then replay
    // its stored response, rather than executing the handler a second time. One semaphore per store key,
    // held for the duration of the request; ref-counted so the entry is removed once its last holder
    // leaves - the dictionary never grows past the number of keys actually in flight right now.
    private static readonly ConcurrentDictionary<string, KeyLock> InFlightLocks = new();

    internal static int InFlightLockCount => InFlightLocks.Count;

    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <remarks>
    /// The store arrives per invocation rather than through the constructor: middleware is a singleton, and a
    /// store backed by a database needs a connection scoped to the request.
    /// </remarks>
    public async Task InvokeAsync(HttpContext context, IIdempotencyStore store)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            await _next(context);

            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var headerValues)
            || !Guid.TryParse(headerValues.ToString(), out var key))
        {
            await _next(context);

            return;
        }

        // The store is keyed per user so one user's replay can never surface another user's response.
        // An anonymous request has no scope to key by - it passes through unstored.
        var userId = UserId(context.User);

        if (userId is null)
        {
            await _next(context);

            return;
        }

        var storeKey = $"idempotency:{userId}:{key:N}";

        if (await store.TryGetAsync(storeKey, context.RequestAborted) is { } stored)
        {
            await ReplayAsync(context, stored, key, userId);

            return;
        }

        var keyLock = Enter(storeKey);

        try
        {
            await keyLock.Semaphore.WaitAsync(context.RequestAborted);
        }
        catch
        {
            Exit(storeKey, keyLock);

            throw;
        }

        try
        {
            // A concurrent request holding the lock may have already run the handler and stored its
            // response while this one waited - replay that instead of running the handler again.
            if (await store.TryGetAsync(storeKey, context.RequestAborted) is { } storedAfterWait)
            {
                await ReplayAsync(context, storedAfterWait, key, userId);

                return;
            }

            // Buffer the response so a successful body can be stored AND still reach the client.
            var originalBody = context.Response.Body;
            byte[] bytes;

            await using (MemoryStream memoryBody = new())
            {
                context.Response.Body = memoryBody;

                try
                {
                    await _next(context);
                }
                finally
                {
                    // Restore even when the handler throws - an upstream exception handler still needs
                    // to write to the real response, not the buffer this method is about to dispose.
                    context.Response.Body = originalBody;
                }

                memoryBody.Seek(0, SeekOrigin.Begin);
                bytes = memoryBody.ToArray();
                memoryBody.Seek(0, SeekOrigin.Begin);
                await memoryBody.CopyToAsync(originalBody);
            }

            // Only a completed write is safe to replay; an error response must stay retryable.
            if (context.Response.StatusCode is >= 200 and < 300)
                await store.SetAsync(storeKey,
                    new IdempotentResponse
                    {
                        StatusCode = context.Response.StatusCode,
                        ContentType = context.Response.ContentType ?? "application/json",
                        Body = bytes
                    },
                    ResponseLifetime,
                    // Not context.RequestAborted: the client that just got a successful response can
                    // disconnect the instant it receives it, which must not cancel recording that success -
                    // that is exactly the case a retry from a timed-out client is supposed to replay.
                    CancellationToken.None);
        }
        finally
        {
            keyLock.Semaphore.Release();
            Exit(storeKey, keyLock);
        }
    }

    private async Task ReplayAsync(HttpContext context, IdempotentResponse stored, Guid key, string userId)
    {
        _logger.LogInformation("Idempotency hit {IdempotencyKey} for user {UserId}", key, userId);
        context.Response.StatusCode = stored.StatusCode;
        context.Response.ContentType = stored.ContentType;
        await context.Response.Body.WriteAsync(stored.Body);
    }

    // JWT keeps the standard "sub" claim (inbound mapping disabled); cookie auth maps the user id to
    // NameIdentifier. Identity.Name is the last resort for exotic schemes.
    private static string? UserId(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated != true
            ? null
            : user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.Identity.Name;

    // Ref-counted so the dictionary entry can be removed the instant its last holder leaves, without a
    // race where a thread starts using an entry that another thread has already begun to remove.
    private static KeyLock Enter(string key)
    {
        while (true)
        {
            var keyLock = InFlightLocks.GetOrAdd(key, static _ => new KeyLock());

            lock (keyLock)
            {
                if (keyLock.RefCount < 0)
                    continue; // Removed by another thread between GetOrAdd and this lock - retry with a fresh entry.

                keyLock.RefCount++;

                return keyLock;
            }
        }
    }

    private static void Exit(string key, KeyLock keyLock)
    {
        lock (keyLock)
        {
            keyLock.RefCount--;

            if (keyLock.RefCount != 0)
                return;

            keyLock.RefCount = -1; // Mark removed before releasing the monitor so a racing Enter retries instead of reusing it.
            InFlightLocks.TryRemove(key, out _);
        }
    }

    private sealed class KeyLock
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int RefCount;
    }
}
