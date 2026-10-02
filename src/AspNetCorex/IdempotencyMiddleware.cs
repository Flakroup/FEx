using FEx.AspNetCorex.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
/// <para>
/// The response is buffered so it can be both stored and sent, up to
/// <see cref="IdempotencyOptions.MaxStoredResponseBytes" /> (1 MiB by default). A larger response streams
/// through to the client untruncated but is not stored, so its retry executes again.
/// </para>
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
    private readonly long _maxStoredResponseBytes;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
        : this(next, logger, Options.Create(new IdempotencyOptions()))
    {
    }

    [ActivatorUtilitiesConstructor]
    public IdempotencyMiddleware(
        RequestDelegate next,
        ILogger<IdempotencyMiddleware> logger,
        IOptions<IdempotencyOptions> options)
    {
        _next = next;
        _logger = logger;
        _maxStoredResponseBytes = Math.Max(0, options.Value.MaxStoredResponseBytes);
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

            // Buffer the response so a successful body can be stored AND still reach the client - but only
            // up to the cap: past it the buffer spills to the client and the rest streams straight through.
            var originalBody = context.Response.Body;
            byte[]? bytes;

            await using (CappedBufferStream buffer = new(originalBody, _maxStoredResponseBytes))
            {
                context.Response.Body = buffer;

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

                bytes = buffer.BufferedBytes();
                await buffer.CompleteAsync();
            }

            // Only a completed write is safe to replay; an error response must stay retryable.
            if (context.Response.StatusCode is < 200 or >= 300)
                return;

            if (bytes is null)
            {
                _logger.LogInformation(
                    "Idempotency response for {IdempotencyKey} (user {UserId}) exceeded {MaxStoredResponseBytes} bytes; sent but not stored, so a retry executes again",
                    key,
                    userId,
                    _maxStoredResponseBytes);

                return;
            }

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

    /// <summary>
    /// Collects the response in memory while it fits under the cap. The first write that would push it
    /// over sends what was buffered to the real body and turns this into a pass-through, so the client
    /// always receives every byte and memory never holds more than the cap.
    /// </summary>
    private sealed class CappedBufferStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _cap;
        private MemoryStream? _buffer = new();
        private long _written;

        public CappedBufferStream(Stream inner, long cap)
        {
            _inner = inner;
            _cap = cap;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _written;

        public override long Position
        {
            get => _written;
            set => throw new NotSupportedException();
        }

        /// <summary>The whole body, or null once it outgrew the cap and was streamed through.</summary>
        public byte[]? BufferedBytes() => _buffer?.ToArray();

        /// <summary>Sends a body that stayed under the cap to the real response.</summary>
        public async Task CompleteAsync()
        {
            if (_buffer is null)
                return;

            _buffer.Position = 0;
            await _buffer.CopyToAsync(_inner);
        }

        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

        public override void Write(ReadOnlySpan<byte> source)
        {
            if (TryBuffer(source))
                return;

            if (_buffer is not null)
            {
                _buffer.Position = 0;
                _buffer.CopyTo(_inner);
                DropBuffer();
            }

            _inner.Write(source);
            _written += source.Length;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            if (TryBuffer(source.Span))
                return;

            if (_buffer is not null)
            {
                _buffer.Position = 0;
                await _buffer.CopyToAsync(_inner, cancellationToken);
                DropBuffer();
            }

            await _inner.WriteAsync(source, cancellationToken);
            _written += source.Length;
        }

        // While buffering, a flush must not reach the real body: it would start the response early.
        public override void Flush()
        {
            if (_buffer is null)
                _inner.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            _buffer is null ? _inner.FlushAsync(cancellationToken) : Task.CompletedTask;

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                DropBuffer();

            base.Dispose(disposing);
        }

        private bool TryBuffer(ReadOnlySpan<byte> source)
        {
            if (_buffer is null || _written + source.Length > _cap)
                return false;

            _buffer.Write(source);
            _written += source.Length;

            return true;
        }

        private void DropBuffer()
        {
            _buffer?.Dispose();
            _buffer = null;
        }
    }
}