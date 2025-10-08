using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Json.Extensions;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FExUriExtensions = FEx.Agnostics.Abstractions.Extensions.Web.UriExtensions;

namespace FEx.Webx.Extensions;

public static class JsonExtensions
{
    public static async Task<T> DeserializeRemoteJsonAsync<T>(this Uri url,
                                                              JsonSerializerSettings settings = null,
                                                              bool checkNetAvailability = false,
                                                              CancellationToken cancellationToken = default)
    {
        //
        T res = default;

        if (!checkNetAvailability
            || await FExUriExtensions.CheckForInternetConnectionAsync(null))
        {
            if (url.Scheme is "http" or "https")
            {
                using var client = new HttpClient();
                using HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
                using HttpResponseMessage ensuredResponse = response.EnsureSuccessStatusCode();
#if NETSTANDARD
                using Stream jsonStream = await ensuredResponse.Content.ReadAsStreamAsync();
#else
                await using Stream jsonStream = await ensuredResponse.Content.ReadAsStreamAsync(cancellationToken);
#endif
                if (jsonStream is not null)
                    res = jsonStream.DeserializeFromStream<T>(settings);
            }
            else
            {
                using WebResponse response = await url.GetUriResponseAsync();
#if NETSTANDARD
                using Stream jsonStream = response.GetResponseStream();
#else
                await using Stream jsonStream = response.GetResponseStream();
#endif
                if (jsonStream is not null)
                    res = jsonStream.DeserializeFromStream<T>(settings);
            }
        }

        return res;
    }
}