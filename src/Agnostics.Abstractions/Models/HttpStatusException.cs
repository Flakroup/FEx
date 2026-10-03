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
    public HttpStatusCode ResponseStatusCode { get; }

    public HttpStatusException(HttpStatusCode statusCode, Uri? url, string? reasonPhrase)
        : base(
            $"Response status code does not indicate success: {(int)statusCode} ({reasonPhrase}) for {Redact(url)}")
    {
        ResponseStatusCode = statusCode;
    }

    // Query strings may carry tokens, so only the scheme, server and path end up in the message.
    private static string? Redact(Uri? url) =>
        url?.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped);
}
