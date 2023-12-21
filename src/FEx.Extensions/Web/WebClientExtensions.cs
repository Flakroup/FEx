using FEx.Extensions.Base.Enums;
using FEx.Extensions.Base.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace FEx.Extensions.Web;

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
            ? new HttpClient(handler)
            : new HttpClient();

        if (resultAsJson)
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(MediaTypes.ApplicationJson.GetEnumValueDescription()));

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