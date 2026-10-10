using FEx.Offline.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
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

    /// <summary>An answer carrying one response header, e.g. the original status a server-side idempotency layer reports.</summary>
    public void EnqueueResponse(HttpStatusCode status, string headerName, string headerValue) =>
        _script.Enqueue(_ =>
        {
            HttpResponseMessage response = new(status)
            {
                Content = new StringContent("")
            };

            response.Headers.TryAddWithoutValidation(headerName, headerValue);

            return response;
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

/// <summary>
/// A loopback HTTP server over raw sockets, so a real <see cref="HttpClient" /> follows (or does not follow) real redirects.
/// The route maps "METHOD /path" to a status, a Location header (or null) and a body.
/// </summary>
internal sealed class RawHttpServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly Func<string, (int Status, string? Location, string Body)> _route;
    private readonly List<string> _requests = [];

    public RawHttpServer(Func<string, (int Status, string? Location, string Body)> route)
    {
        _route = route;
        _listener.Start();
        _ = Task.Run(AcceptLoopAsync); // ends when Dispose stops the listener
    }

    public Uri BaseAddress => new($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/");

    /// <summary>Every request line's "METHOD /path", in arrival order.</summary>
    public IReadOnlyList<string> Requests
    {
        get
        {
            lock (_requests)
                return [.. _requests];
        }
    }

    public void Dispose() => _listener.Stop();

    private async Task AcceptLoopAsync()
    {
        while (true)
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync();

                await ServeQuietlyAsync(client.GetStream());
            }
            catch (Exception e) when (e is SocketException or ObjectDisposedException or InvalidOperationException)
            {
                return; // the listener was stopped
            }
        }
    }

    // One broken connection (a client that dropped, a request line this server cannot parse) must not end the accept loop:
    // the next test request would then hang instead of failing.
    private async Task ServeQuietlyAsync(NetworkStream stream)
    {
        try
        {
            await ServeAsync(stream);
        }
        catch (Exception)
        {
            // See above.
        }
    }

    private async Task ServeAsync(NetworkStream stream)
    {
        StringBuilder head = new();
        var one = new byte[1];

        while (!head.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal) && await stream.ReadAsync(one) == 1)
            head.Append((char)one[0]);

        var lines = head.ToString().Split("\r\n");
        var requestLine = lines[0].Split(' ');
        var key = $"{requestLine[0]} {requestLine[1]}";

        // Drain the body so the client is not reset while it is still writing it.
        var length = lines.Where(l => l.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                          .Select(l => int.Parse(l[15..].Trim()))
                          .FirstOrDefault();

        await stream.ReadExactlyAsync(new byte[length]);

        lock (_requests)
            _requests.Add(key);

        var (status, location, body) = _route(key);
        var response = $"HTTP/1.1 {status} X\r\n"
                       + (location is null ? "" : $"Location: {location}\r\n")
                       + $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\nConnection: close\r\n\r\n{body}";

        await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
    }
}