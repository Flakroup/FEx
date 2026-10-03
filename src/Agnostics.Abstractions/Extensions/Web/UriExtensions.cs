using FEx.Agnostics.Abstractions.Models;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Extensions.Web;

public static class UriExtensions
{
    private const string HttpScheme = "http";
    private const string HttpsScheme = "https";
    /// <summary>The time to wait for response headers when <see cref="WebRequestParams.Timeout" /> is not set, like <c>HttpWebRequest</c>.</summary>
    internal const int DefaultTimeoutMilliseconds = 100_000;

    private static Uri DefaultUri { get; } = new("http://clients3.google.com/generate_204");

    /// <summary>
    /// Checks connectivity by requesting <paramref name="url" /> (the default connectivity probe when it is null or not
    /// an HTTP(S) URL).
    /// </summary>
    public static async Task<bool> CheckForInternetConnectionAsync(this Uri url,
                                                                   HttpClient? client = null,
                                                                   CancellationToken cancellationToken = default)
    {
        url ??= DefaultUri;

        if (url.Scheme is not (HttpScheme or HttpsScheme))
            url = DefaultUri;

        try
        {
            using var _ = await url.SendHttpAsync(client: client, cancellationToken: cancellationToken);

            return true;
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <summary>
    /// Sends an HTTP(S) request with <see cref="HttpCompletionOption.ResponseHeadersRead" /> so the body is not
    /// buffered. The caller owns (and must dispose) the returned response.
    /// </summary>
    /// <param name="url">The target URL.</param>
    /// <param name="pars">Method, headers, user agent, timeout and handler-level settings (see <see cref="HttpClientProvider" />).</param>
    /// <param name="client">
    /// The client to use. When null, a long-lived shared client matching <paramref name="pars" /> is used; pass your own
    /// (e.g. one from an <c>IHttpClientFactory</c>) to control the handler.
    /// </param>
    /// <param name="range">Optional byte range, sent as a <c>Range</c> header.</param>
    /// <param name="ensureSuccess">When true, a non-success status throws <see cref="HttpStatusException" />.</param>
    /// <param name="method">Overrides <see cref="WebRequestParams.Method" /> for this request only, leaving <paramref name="pars" /> untouched.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public static async Task<HttpResponseMessage> SendHttpAsync(this Uri url,
                                                                WebRequestParams? pars = null,
                                                                HttpClient? client = null,
                                                                RangeHeaderValue? range = null,
                                                                bool ensureSuccess = true,
                                                                HttpMethod? method = null,
                                                                CancellationToken cancellationToken = default) =>
        await url.SendHttpAsync(pars, client, range, ensureSuccess, DefaultTimeoutMilliseconds, method, cancellationToken);

    internal static async Task<HttpResponseMessage> SendHttpAsync(this Uri url,
                                                                  WebRequestParams? pars,
                                                                  HttpClient? client,
                                                                  RangeHeaderValue? range,
                                                                  bool ensureSuccess,
                                                                  int defaultTimeoutMilliseconds,
                                                                  HttpMethod? method,
                                                                  CancellationToken cancellationToken)
    {
        client ??= HttpClientProvider.Get(pars);

        using var request = CreateHttpRequest(url, pars, range, method);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Like HttpWebRequest.Timeout (default 100 s, -1 = infinite), the timeout covers waiting for the response
        // headers only.
        var timeout = pars?.Timeout ?? defaultTimeoutMilliseconds;

        if (timeout > 0)
            timeoutSource.CancelAfter(timeout);

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token);

        if (ensureSuccess
            && !response.IsSuccessStatusCode)
        {
            var status = response.StatusCode;
            var reason = response.ReasonPhrase;
            var retryAfter = GetRetryAfter(response);
            response.Dispose();

            throw new HttpStatusException(status, url, reason, retryAfter);
        }

        return response;
    }

    public static async Task<FileWebResponse> GetUriFileResponseAsync(this Uri url,
                                                                      WebRequestParams? pars = null,
                                                                      Stopwatch? stopwatch = null) =>
        (FileWebResponse)await url.GetUriResponseAsync(pars, stopwatch);

    /// <summary>Gets the response of a non-HTTP request (FTP, file). HTTP(S) goes through <see cref="SendHttpAsync" />.</summary>
    public static async Task<WebResponse> GetUriResponseAsync(this Uri url,
                                                              WebRequestParams? pars = null,
                                                              Stopwatch? stopwatch = null)
    {
        var req = url.GetWebRequest(pars);
        stopwatch?.Restart();
        var response = await req.GetResponseAsync();
        stopwatch?.Stop();

        return response;
    }

    /// <summary>
    /// Creates a <see cref="WebRequest" /> for non-HTTP schemes only (ftp, file): <c>FtpWebRequest</c> has no
    /// HttpClient equivalent. HTTP(S) URLs are rejected, use <see cref="SendHttpAsync" />.
    /// </summary>
    public static WebRequest GetWebRequest(this Uri url, WebRequestParams? pars = null)
    {
        if (url.Scheme is HttpScheme or HttpsScheme)
            throw new ArgumentException("HTTP(S) requests must be sent with SendHttpAsync (HttpClient).", nameof(url));

#if NET
        // SYSLIB0014: FtpWebRequest has no HttpClient replacement; only ftp/file URLs reach this call.
#pragma warning disable SYSLIB0014
#endif
        var myWebRequest = WebRequest.Create(url);
#if NET
#pragma warning restore SYSLIB0014
#endif

        if (pars is not null)
            myWebRequest.PrepareRequest(pars);

        return myWebRequest;
    }

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is { } delta)
            return delta;

        return retryAfter?.Date is { } date
            ? date - DateTimeOffset.UtcNow
            : null;
    }

    private static HttpRequestMessage CreateHttpRequest(Uri url,
                                                        WebRequestParams? pars,
                                                        RangeHeaderValue? range,
                                                        HttpMethod? method)
    {
        var request = new HttpRequestMessage(method ?? (pars?.Method is { } name
                ? new(name)
                : HttpMethod.Get),
            url);

        if (pars?.UserAgent is not null)
            request.Headers.TryAddWithoutValidation("User-Agent", pars.UserAgent);

        if (pars?.Headers is not null)
            foreach (var header in pars.Headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (pars?.KeepAlive is { } keepAlive)
            request.Headers.ConnectionClose = !keepAlive;

        request.Headers.Range = range;

        return request;
    }
}
