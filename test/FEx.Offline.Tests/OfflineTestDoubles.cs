using FEx.Offline.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Offline.Tests;

/// <summary>In-memory IKeyValueStore standing in for the browser's localStorage.</summary>
internal sealed class InMemoryKeyValueStore : IKeyValueStore
{
    private readonly ConcurrentDictionary<string, string> _values = new(StringComparer.Ordinal);

    public Task<string?> GetAsync(string key) =>
        Task.FromResult(_values.TryGetValue(key, out var value)
            ? value
            : null);

    public Task SetAsync(string key, string value)
    {
        _values[key] = value;

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _values.TryRemove(key, out _);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetKeysAsync(string prefix) =>
        Task.FromResult<IReadOnlyList<string>>(_values.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .ToList());
}

/// <summary>An IKeyValueStore whose first removal of a live outbox entry fails, as an app kill or a storage error would.</summary>
internal sealed class FailingFirstRemoveStore : IKeyValueStore
{
    private readonly InMemoryKeyValueStore _inner = new();
    private bool _failed;

    public Task<string?> GetAsync(string key) => _inner.GetAsync(key);

    public Task SetAsync(string key, string value) => _inner.SetAsync(key, value);

    public Task<IReadOnlyList<string>> GetKeysAsync(string prefix) => _inner.GetKeysAsync(prefix);

    public Task RemoveAsync(string key)
    {
        if (_failed || !key.StartsWith("outbox:", StringComparison.Ordinal))
            return _inner.RemoveAsync(key);

        _failed = true;

        throw new IOException("simulated storage failure");
    }
}

/// <summary>An IKeyValueStore that refuses to write a dead-letter entry.</summary>
internal sealed class DeadWriteFailingStore : IKeyValueStore
{
    private readonly InMemoryKeyValueStore _inner = new();

    public Task<string?> GetAsync(string key) => _inner.GetAsync(key);

    public Task RemoveAsync(string key) => _inner.RemoveAsync(key);

    public Task<IReadOnlyList<string>> GetKeysAsync(string prefix) => _inner.GetKeysAsync(prefix);

    public Task SetAsync(string key, string value) =>
        key.StartsWith("outbox-dead:", StringComparison.Ordinal)
            ? throw new IOException("simulated storage failure")
            : _inner.SetAsync(key, value);
}

/// <summary>Scripted HttpMessageHandler: replays the queued responses (or throws) per request, in order.</summary>
internal sealed class ScriptedHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _script = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string?> RequestBodies { get; } = [];

    public List<string?> IdempotencyKeys { get; } = [];

    /// <summary>Every request's headers as sent, each multi-valued header joined by a comma. Captured inside
    /// the send, because the outbox disposes each request once its response is in.</summary>
    public List<IReadOnlyDictionary<string, string>> RequestHeaders { get; } = [];

    public void EnqueueResponse(HttpStatusCode status, string body = "") =>
        _script.Enqueue(_ => new(status)
        {
            Content = new StringContent(body)
        });

    public void EnqueueNetworkFailure() => _script.Enqueue(_ => throw new HttpRequestException("connection refused"));

    /// <summary>An answer whose Content-Type charset .NET cannot decode, so reading its body as a string throws.</summary>
    public void EnqueueUnreadableResponse(HttpStatusCode status) =>
        _script.Enqueue(_ =>
        {
            HttpResponseMessage response = new(status)
            {
                Content = new ByteArrayContent([1, 2, 3])
            };

            response.Content.Headers.TryAddWithoutValidation("Content-Type", "text/plain; charset=bogus-charset");

            return response;
        });

    public void EnqueueTimeout() => _script.Enqueue(_ => throw new TaskCanceledException("timed out"));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                                 CancellationToken cancellationToken)
    {
        Requests.Add(request);

        RequestBodies.Add(request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken));

        IdempotencyKeys.Add(request.Headers.TryGetValues("Idempotency-Key", out var values)
            ? string.Join(",", values)
            : null);

        RequestHeaders.Add(request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value),
            StringComparer.OrdinalIgnoreCase));

        if (_script.Count == 0)
            throw new InvalidOperationException("ScriptedHandler ran out of scripted responses.");

        return _script.Dequeue()(request);
    }
}

/// <summary>Fixed clock for deterministic timestamps.</summary>
internal sealed class FixedTime : TimeProvider
{
    private DateTimeOffset _now;

    public FixedTime(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}