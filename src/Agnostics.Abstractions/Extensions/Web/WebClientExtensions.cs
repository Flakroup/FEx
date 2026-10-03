using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Models;
using System;
using System.Net.Http;

namespace FEx.Agnostics.Abstractions.Extensions.Web;

/// <summary>Extensions for configuring <see cref="System.Net.Http.HttpClient" /> instances.</summary>
public static class WebClientExtensions
{
    /// <summary>Creates an <see cref="System.Net.Http.HttpClient" /> configured from the request parameters.</summary>
    /// <param name="client">Receives the new client, which owns its handler.</param>
    /// <param name="pars">The parameters supplying handler settings, timeout, user agent, headers and keep-alive.</param>
    /// <param name="resultAsJson">Whether to add an <c>Accept: application/json</c> header.</param>
    public static void PrepareHttpClient(out HttpClient client, WebRequestParams pars, bool resultAsJson = false)
    {
#pragma warning disable IDISP001 // handler ownership transferred to HttpClient
        var handler = pars.GetHttpClientHandler();
#pragma warning restore IDISP001

        client = handler is not null
            ? new(handler)
            : new HttpClient();

        if (resultAsJson)
            client.DefaultRequestHeaders.Accept.Add(new(MediaTypes.ApplicationJson.GetEnumValueDescription()!));

        if (pars?.Timeout is not null)
            client.Timeout = TimeSpan.FromMilliseconds(pars.Timeout.Value);

        if (pars?.UserAgent is not null)
            client.DefaultRequestHeaders.Add("User-Agent", pars.UserAgent);

        if (pars?.Headers is not null)
            foreach (var header in pars.Headers)
                client.DefaultRequestHeaders.Add(header.Key, header.Value);

        if (pars?.KeepAlive.HasValue == true)
            client.DefaultRequestHeaders.ConnectionClose = !pars.KeepAlive.Value;
    }
}