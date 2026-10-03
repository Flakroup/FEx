using System;
using System.Net;
using System.Net.Http;

namespace FEx.Agnostics.Abstractions.Models;

/// <summary>
/// Thrown when an HTTP response carries a non-success status code. Exposes the status on every target framework
/// (<c>HttpRequestException.StatusCode</c> exists only on .NET 5+).
/// </summary>
public sealed class HttpStatusException : HttpRequestException
{
    /// <summary>Gets the HTTP status code of the failed response.</summary>
    public HttpStatusCode ResponseStatusCode { get; }

    /// <summary>The delay the server asked for with a <c>Retry-After</c> header, if any.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>Initializes the exception for a failed response; only the scheme, server and path of the URL are put in the message</summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="url">The requested URL.</param>
    /// <param name="reasonPhrase">The reason phrase of the response.</param>
    /// <param name="retryAfter">The delay requested by the server, if any.</param>
    public HttpStatusException(HttpStatusCode statusCode, Uri? url, string? reasonPhrase, TimeSpan? retryAfter = null)
        : base(
            $"Response status code does not indicate success: {(int)statusCode} ({reasonPhrase}) for {Redact(url)}")
    {
        ResponseStatusCode = statusCode;
        RetryAfter = retryAfter;
    }

    // Query strings and user info may carry tokens, so only the scheme, server and path end up in the message.
    private static string? Redact(Uri? url) =>
        url?.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped);
}
