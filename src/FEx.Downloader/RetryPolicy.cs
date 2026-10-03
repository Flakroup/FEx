using FEx.Agnostics.Abstractions.Models;
using System;
using System.Net;

namespace FEx.Downloader;

/// <summary>Which HTTP failures are worth retrying, and how long to wait before doing so.</summary>
internal static class RetryPolicy
{
    internal const int DefaultMaxRetries = 5;
    internal static readonly TimeSpan DefaultBaseDelay = TimeSpan.FromMilliseconds(500);
    internal static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(60);

    /// <summary>Request timeout, rate limiting and server errors may succeed on a later attempt; other statuses will not.</summary>
    internal static bool IsTransient(HttpStatusCode status) =>
        status is HttpStatusCode.RequestTimeout or (HttpStatusCode)429 || (int)status >= 500;

    /// <summary>Honours <c>Retry-After</c> when present, otherwise doubles <paramref name="baseDelay" /> per attempt; capped at <see cref="MaxDelay" />.</summary>
    internal static TimeSpan GetDelay(HttpStatusException error, int attempt, TimeSpan baseDelay)
    {
        var delay = error.RetryAfter is { } retryAfter && retryAfter > TimeSpan.Zero
            ? retryAfter
            : TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, Math.Min(Math.Max(attempt - 1, 0), 16)));

        return delay > MaxDelay
            ? MaxDelay
            : delay;
    }
}
