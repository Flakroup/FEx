using FEx.Agnostics.Abstractions.Models;
using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FEx.Agnostics.Abstractions.Extensions.Web;

/// <summary>
/// Hands out long-lived <see cref="HttpClient" /> instances: handler-level settings (credentials, cookies, proxy,
/// certificate validation) live on the handler, so each <see cref="WebRequestParams" /> instance that needs them gets
/// one client for its lifetime, everything else shares a single default client. Per-request settings (method, headers,
/// timeout) are applied to the request message instead.
/// </summary>
internal static class HttpClientProvider
{
    private static readonly Lazy<HttpClient> Shared = new(() => Create(null), LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly ConditionalWeakTable<WebRequestParams, HttpClient> PerParams = new();

    public static HttpClient Get(WebRequestParams? pars) =>
        pars is null || !NeedsOwnHandler(pars)
            ? Shared.Value
            : PerParams.GetValue(pars, Create);

    private static bool NeedsOwnHandler(WebRequestParams pars) =>
        pars.Credentials is not null
        || pars.Cookies is not null
        || pars.Proxy is not null
        || pars.IsProxyNull
        || pars.ServerCertificateValidationCallback is not null;

    private static HttpClient Create(WebRequestParams? pars)
    {
#pragma warning disable IDISP001 // handler ownership transferred to HttpClient, which lives for the process lifetime
        var handler = pars?.GetHttpClientHandler() ?? new HttpClientHandler();
#pragma warning restore IDISP001

        // Timeouts are applied per request through a CancellationToken, not through the client-wide Timeout.
        return new(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
    }
}
