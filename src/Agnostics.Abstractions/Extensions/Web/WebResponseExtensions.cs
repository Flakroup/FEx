using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Extensions.Web;

/// <summary>Extensions for reading range and header information from HTTP responses.</summary>
public static class WebResponseExtensions
{
    /// <summary>The name of the <c>Content-Range</c> response header.</summary>
    public const string ContentRangeHeaderName = "Content-Range";
    /// <summary>The name of the <c>Accept-Ranges</c> response header.</summary>
    public const string AcceptRangesHeaderName = "Accept-Ranges";
    private const string BytesRangeUnit = "bytes";

    /// <summary>
    /// Probes whether the server honours byte ranges: <paramref name="response" /> must advertise
    /// <c>Accept-Ranges: bytes</c> and a ranged request must come back with a <c>Content-Range</c>.
    /// </summary>
    public static async Task<(bool, LengthType)> TryGetRangeAsync(this Uri responseUri,
                                                                  HttpResponseMessage response,
                                                                  long rangeFrom,
                                                                  long rangeTo,
                                                                  WebRequestParams? pars = null,
                                                                  HttpClient? client = null,
                                                                  CancellationToken cancellationToken = default)
    {
        if (!response.Headers.AcceptRanges.Any(x => string.Equals(x, BytesRangeUnit, StringComparison.OrdinalIgnoreCase)))
            return (false, LengthType.AutoDetect);

        using var rangeResponse = await responseUri.SendHttpAsync(pars,
            client,
            new(rangeFrom, rangeTo),
            cancellationToken: cancellationToken);

        return (rangeResponse.Content.Headers.ContentRange is not null, LengthType.Bytes);
    }

    /// <summary>Gets the <c>Content-Range</c> header of a response.</summary>
    /// <param name="response">The response.</param>
    /// <returns>The header value, or null when absent.</returns>
    public static ContentRangeHeaderValue? GetContentRange(this HttpResponseMessage response) =>
        response.Content.Headers.ContentRange;

    /// <summary>Parses a <c>Content-Range</c> header text such as <c>bytes 0-99/1000</c>.</summary>
    /// <param name="rangeHeader">The header text.</param>
    /// <returns>The parsed range, or null when the text is null or blank.</returns>
    public static ContentRangeHeaderValue? GetContentRange(this string? rangeHeader)
    {
        if (rangeHeader?.Trim().IsNullOrEmptyString() ?? true)
            return null;

        var split = rangeHeader.Split(' ')[1].Split('/')[0].Split('-');
        var from = long.Parse(split[0]);
        var to = long.Parse(split[1]);

        return new(from, to);
    }

    /// <summary>Gets the response headers together with the content headers (Content-Length, Content-Disposition, ...).</summary>
    public static Dictionary<string, string[]> GetAllHeaders(this HttpResponseMessage resp)
    {
        KeyValuePair<string, IEnumerable<string>>[] headers = [.. resp.Headers, .. resp.Content.Headers];

        return headers.ToDictionary(x => x.Key, x => x.Value.ToArray());
    }
}
