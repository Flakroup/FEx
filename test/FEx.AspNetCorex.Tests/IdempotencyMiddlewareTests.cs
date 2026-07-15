using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FEx.AspNetCorex.Tests;

/// <summary>
/// The idempotency contract an offline outbox relies on: a retried POST with the same key replays
/// the completed response instead of executing the write again - scoped per user, successful
/// responses only, everything else passes through untouched.
/// </summary>
public sealed class IdempotencyMiddlewareTests
{
    private static readonly Guid Key = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");

    [Fact]
    public async Task RetriedPost_WithSameUserAndKey_ReplaysResponseWithoutSecondExecution()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        var executions = 0;

        var middleware = Middleware(cache,
            async context =>
            {
                executions++;
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync($"{{\"id\":{executions}}}");
            });

        var first = PostContext(Key);
        await middleware.InvokeAsync(first);
        var second = PostContext(Key);
        await middleware.InvokeAsync(second);

        executions.ShouldBe(1);
        second.Response.StatusCode.ShouldBe(StatusCodes.Status201Created);
        second.Response.ContentType.ShouldBe("application/json; charset=utf-8");
        Body(second).ShouldBe("{\"id\":1}");
    }

    [Fact]
    public async Task FirstExecution_StillWritesTheResponseToTheClient()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        var middleware = Middleware(cache, context => context.Response.WriteAsync("payload"));

        var context = PostContext(Key);
        await middleware.InvokeAsync(context);

        Body(context).ShouldBe("payload");
    }

    [Fact]
    public async Task DifferentKey_ExecutesAgain()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        var executions = 0;

        var middleware = Middleware(cache,
            _ =>
            {
                executions++;

                return Task.CompletedTask;
            });

        await middleware.InvokeAsync(PostContext(Key));
        await middleware.InvokeAsync(PostContext(Guid.Parse("00000000-0000-0000-0000-000000000001")));

        executions.ShouldBe(2);
    }

    [Fact]
    public async Task SameKey_DifferentUser_ExecutesAgain_AndNeverLeaksTheOtherUsersResponse()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        var executions = 0;

        var middleware = Middleware(cache,
            async context =>
            {
                executions++;
                await context.Response.WriteAsync($"user-specific-{executions}");
            });

        await middleware.InvokeAsync(PostContext(Key, "user-a"));
        var other = PostContext(Key, "user-b");
        await middleware.InvokeAsync(other);

        executions.ShouldBe(2);
        Body(other).ShouldBe("user-specific-2");
    }

    [Fact]
    public async Task NameIdentifierClaim_ScopesTheCache_LikeTheSubClaim()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        var executions = 0;

        var middleware = Middleware(cache,
            _ =>
            {
                executions++;

                return Task.CompletedTask;
            });

        // Cookie-authenticated principals carry the user id in NameIdentifier, not "sub".
        ClaimsPrincipal cookieUser = new(new ClaimsIdentity(
            [new(ClaimTypes.NameIdentifier, "cookie-user")],
            "cookies"));

        var first = PostContext(Key);
        first.User = cookieUser;
        var second = PostContext(Key);
        second.User = cookieUser;

        await middleware.InvokeAsync(first);
        await middleware.InvokeAsync(second);

        executions.ShouldBe(1);
    }

    [Fact]
    public async Task NonPost_PassesThrough_EvenWithAKey()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        var executions = 0;

        var middleware = Middleware(cache,
            _ =>
            {
                executions++;

                return Task.CompletedTask;
            });

        var first = PostContext(Key);
        first.Request.Method = HttpMethods.Get;
        var second = PostContext(Key);
        second.Request.Method = HttpMethods.Get;

        await middleware.InvokeAsync(first);
        await middleware.InvokeAsync(second);

        executions.ShouldBe(2);
    }

    [Fact]
    public async Task MissingOrMalformedKey_PassesThrough()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        var executions = 0;

        var middleware = Middleware(cache,
            _ =>
            {
                executions++;

                return Task.CompletedTask;
            });

        var withoutHeader = PostContext(null);
        var malformed = PostContext(null);
        malformed.Request.Headers[IdempotencyMiddleware.HeaderName] = "not-a-guid";

        await middleware.InvokeAsync(withoutHeader);
        await middleware.InvokeAsync(malformed);
        await middleware.InvokeAsync(PostContext(null));

        executions.ShouldBe(3);
    }

    [Fact]
    public async Task AnonymousRequest_PassesThrough_AndIsNeverCached()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        var executions = 0;

        var middleware = Middleware(cache,
            _ =>
            {
                executions++;

                return Task.CompletedTask;
            });

        await middleware.InvokeAsync(PostContext(Key, null));
        await middleware.InvokeAsync(PostContext(Key, null));

        executions.ShouldBe(2);
        cache.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ErrorResponse_IsNotCached_SoTheRetryExecutesAgain()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        var executions = 0;

        var middleware = Middleware(cache,
            context =>
            {
                executions++;

                context.Response.StatusCode = executions == 1
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status200OK;

                return Task.CompletedTask;
            });

        await middleware.InvokeAsync(PostContext(Key));
        var retry = PostContext(Key);
        await middleware.InvokeAsync(retry);

        executions.ShouldBe(2);
        retry.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    // --- Harness ----------------------------------------------------------------------------------

    private static IdempotencyMiddleware Middleware(IMemoryCache cache, RequestDelegate next) =>
        new(next, cache, NullLogger<IdempotencyMiddleware>.Instance);

    private static DefaultHttpContext PostContext(Guid? key, string? userId = "user-1")
    {
        DefaultHttpContext context = new();
        context.Request.Method = HttpMethods.Post;

        if (key is { } k)
            context.Request.Headers[IdempotencyMiddleware.HeaderName] = k.ToString();

        if (userId is not null)
            context.User = new(new ClaimsIdentity([new("sub", userId)], "test"));

        context.Response.Body = new MemoryStream();

        return context;
    }

    private static string Body(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8, leaveOpen: true);

        return reader.ReadToEnd();
    }
}