using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;

namespace FEx.Extensions.Web;

public static class WebClientExtensions
{
    public static void PrepareWebClient(this WebClient client, WebRequestParams pars)
    {
        if (pars?.Credentials != null)
        {
            client.Credentials = pars.Credentials;
        }

        if (pars?.UserAgent != null)
        {
            client.Headers.Add("User-Agent", pars.UserAgent);
        }

        if (pars?.Headers != null)
        {
            foreach (KeyValuePair<string, string> header in pars.Headers)
            {
                client.Headers.Add(header.Key, header.Value);
            }
        }
    }

    [SuppressMessage("Wrong Usage", "DF0010:Marks undisposed local variables.")]
    public static void PrepareHttpClient(out HttpClient client, WebRequestParams pars, bool resultAsJson = false)
    {
        HttpClientHandler handler = pars.GetHttpClientHandler();

        client = handler != null ? new HttpClient(handler) : new HttpClient();

        if (resultAsJson)
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypes.ApplicationJson.GetEnumValueDescription()));
        }

        if (pars?.Timeout != null)
        {
            client.Timeout = TimeSpan.FromMilliseconds(pars.Timeout.Value);
        }

        if (pars?.UserAgent != null)
        {
            client.DefaultRequestHeaders.Add("User-Agent", pars.UserAgent);
        }

        if (pars?.Headers != null)
        {
            foreach (KeyValuePair<string, string> header in pars.Headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        if (pars?.KeepAlive.HasValue == true)
        {
            client.DefaultRequestHeaders.ConnectionClose = !pars.KeepAlive.Value;
        }
    }
}