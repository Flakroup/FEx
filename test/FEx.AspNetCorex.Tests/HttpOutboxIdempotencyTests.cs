using FEx.AspNetCorex.Abstractions;
using FEx.Offline;
using FEx.Offline.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.AspNetCorex.Tests;

/// <summary>
/// <see cref="HttpOutbox" /> replaying through the real <see cref="IdempotencyMiddleware" />: a write that landed
/// but whose response was lost, with a response too large for the middleware to store, is answered 409 + the
/// original status on the replay. The outbox must read that as delivered, not as a rejection.
/// </summary>
// Runs the middleware, so it shares the static in-flight lock dictionary with the other middleware tests.
[Collection(IdempotencyLockCollection.Name)]
public sealed class HttpOutboxIdempotencyTests
{
    [Fact]
    public async Task WriteWhoseOverCapResponseWasLost_IsDeliveredOnReplay_NotDeadLettered_AndExecutedOnce()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        var executions = 0;
        IdempotencyMiddleware middleware = new(
            async context =>
            {
                executions++;
                context.Response.StatusCode = StatusCodes.Status201Created;
                await context.Response.WriteAsync("a response longer than the 4-byte cap");
            },
            NullLogger<IdempotencyMiddleware>.Instance,
            Options.Create(new IdempotencyOptions { MaxStoredResponseBytes = 4 }));

        using MiddlewareHandler handler = new(middleware, new MemoryCacheIdempotencyStore(cache));
        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        HttpOutbox outbox = new(new DictionaryKeyValueStore(), TimeProvider.System);
        await outbox.EnqueueAsync("POST", "api/payments", """{"amount":5}""");

        handler.LoseNextResponse = true;
        var first = await outbox.FlushAsync(http);

        first.Sent.ShouldBe(0);
        first.Remaining.ShouldBe(1); // the response never arrived: the entry stays queued

        var replay = await outbox.FlushAsync(http);

        handler.LastStatus.ShouldBe(StatusCodes.Status409Conflict);
        replay.ShouldBe(new(1, 0, 0, null));
        (await outbox.ListDeadAsync()).ShouldBeEmpty();
        executions.ShouldBe(1);
    }

    /// <summary>Hands each request to the middleware as the server would, and can drop the response on the way back.</summary>
    private sealed class MiddlewareHandler : HttpMessageHandler
    {
        private readonly IdempotencyMiddleware _middleware;
        private readonly IIdempotencyStore _store;

        public MiddlewareHandler(IdempotencyMiddleware middleware, IIdempotencyStore store)
        {
            _middleware = middleware;
            _store = store;
        }

        public bool LoseNextResponse { get; set; }

        public int LastStatus { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            DefaultHttpContext context = new();
            context.Request.Method = request.Method.Method;
            context.Request.Headers[IdempotencyMiddleware.HeaderName] = request.Headers.GetValues(IdempotencyMiddleware.HeaderName).Single();
            context.User = new(new ClaimsIdentity([new("sub", "user-1")], "test"));
            context.Response.Body = new MemoryStream();

            await _middleware.InvokeAsync(context, _store);

            LastStatus = context.Response.StatusCode;

            if (LoseNextResponse)
            {
                LoseNextResponse = false;

                throw new HttpRequestException("the response was lost on the way back");
            }

            context.Response.Body.Position = 0;
            HttpResponseMessage response = new((HttpStatusCode)context.Response.StatusCode)
            {
                Content = new StreamContent(context.Response.Body)
            };

            foreach (var (name, values) in context.Response.Headers)
                response.Headers.TryAddWithoutValidation(name, (IEnumerable<string>)values!);

            return response;
        }
    }

    private sealed class DictionaryKeyValueStore : IKeyValueStore
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

        public Task<string?> GetAsync(string key) => Task.FromResult(_values.GetValueOrDefault(key));

        public Task SetAsync(string key, string value)
        {
            _values[key] = value;

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _values.Remove(key);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> GetKeysAsync(string prefix) =>
            Task.FromResult<IReadOnlyList<string>>(_values.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList());
    }
}
