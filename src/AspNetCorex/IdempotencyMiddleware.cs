using FEx.AspNetCorex.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Claims;
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
/// </remarks>
public sealed class IdempotencyMiddleware
{
    /// <summary>The request header carrying the client-generated idempotency key (a GUID).</summary>
    public const string HeaderName = "Idempotency-Key";

    private static readonly TimeSpan ResponseLifetime = TimeSpan.FromHours(24);

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
            _logger.LogInformation("Idempotency hit {IdempotencyKey} for user {UserId}", key, userId);
            context.Response.StatusCode = stored.StatusCode;
            context.Response.ContentType = stored.ContentType;
            await context.Response.Body.WriteAsync(stored.Body);

            return;
        }

        // Buffer the response so a successful body can be stored AND still reach the client.
        var originalBody = context.Response.Body;
        await using MemoryStream memoryBody = new();
        context.Response.Body = memoryBody;

        await _next(context);

        memoryBody.Seek(0, SeekOrigin.Begin);
        var bytes = memoryBody.ToArray();
        memoryBody.Seek(0, SeekOrigin.Begin);
        await memoryBody.CopyToAsync(originalBody);
        context.Response.Body = originalBody;

        // Only a completed write is safe to replay; an error response must stay retryable.
        // ponytail: two concurrent requests with the same key both execute (the store is written only on
        // completion); the outbox retries sequentially, so this stays theoretical. Upgrade path: a
        // per-key in-flight lock.
        if (context.Response.StatusCode is >= 200 and < 300)
            await store.SetAsync(storeKey,
                new IdempotentResponse
                {
                    StatusCode = context.Response.StatusCode,
                    ContentType = context.Response.ContentType ?? "application/json",
                    Body = bytes
                },
                ResponseLifetime,
                context.RequestAborted);
    }

    // JWT keeps the standard "sub" claim (inbound mapping disabled); cookie auth maps the user id to
    // NameIdentifier. Identity.Name is the last resort for exotic schemes.
    private static string? UserId(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated != true
            ? null
            : user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.Identity.Name;
}
