using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace FEx.Agnostics.Abstractions.Extensions.Web;

public static class WebClientExtensions
{
    public static void PrepareWebClient(this WebClient client, WebRequestParams pars)
    {
        if (pars?.Credentials is not null)
            client.Credentials = pars.Credentials;

        if (pars?.UserAgent is not null)
            client.Headers.Add("User-Agent", pars.UserAgent);

        if (pars?.Headers is not null)
            foreach (KeyValuePair<string, string> header in pars.Headers)
                client.Headers.Add(header.Key, header.Value);
    }

    public static void PrepareHttpClient(out HttpClient client, WebRequestParams pars, bool resultAsJson = false)
    {
        HttpClientHandler handler = pars.GetHttpClientHandler();

        client = handler is not null
            ? new(handler)
            : new HttpClient();

        if (resultAsJson)
            client.DefaultRequestHeaders.Accept.Add(new(MediaTypes.ApplicationJson.GetEnumValueDescription()));

        if (pars?.Timeout is not null)
            client.Timeout = TimeSpan.FromMilliseconds(pars.Timeout.Value);

        if (pars?.UserAgent is not null)
            client.DefaultRequestHeaders.Add("User-Agent", pars.UserAgent);

        if (pars?.Headers is not null)
            foreach (KeyValuePair<string, string> header in pars.Headers)
                client.DefaultRequestHeaders.Add(header.Key, header.Value);

        if (pars?.KeepAlive.HasValue == true)
            client.DefaultRequestHeaders.ConnectionClose = !pars.KeepAlive.Value;
    }
}