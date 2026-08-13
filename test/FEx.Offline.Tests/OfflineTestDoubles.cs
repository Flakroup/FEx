using FEx.Offline.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

/// <summary>Scripted HttpMessageHandler: replays the queued responses (or throws) per request, in order.</summary>
internal sealed class ScriptedHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _script = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string?> RequestBodies { get; } = [];

    public List<string?> IdempotencyKeys { get; } = [];

    public void EnqueueResponse(HttpStatusCode status, string body = "") =>
        _script.Enqueue(_ => new(status)
        {
            Content = new StringContent(body)
        });

    public void EnqueueNetworkFailure() => _script.Enqueue(_ => throw new HttpRequestException("connection refused"));

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