using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Json.Extensions;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FExUriExtensions = FEx.Agnostics.Abstractions.Extensions.Web.UriExtensions;

namespace FEx.Webx.Extensions;

public static class JsonExtensions
{
    public static Task<T> DeserializeRemoteJsonAsync<T>(this Uri url) =>
        url.DeserializeRemoteJsonAsync<T>(null, false, default);

    public static Task<T> DeserializeRemoteJsonAsync<T>(this Uri url, JsonSerializerSettings settings) =>
        url.DeserializeRemoteJsonAsync<T>(settings, false, default);

    public static Task<T> DeserializeRemoteJsonAsync<T>(this Uri url,
                                                        JsonSerializerSettings settings,
                                                        bool checkNetAvailability) =>
        url.DeserializeRemoteJsonAsync<T>(settings, checkNetAvailability, default);

    public static async Task<T> DeserializeRemoteJsonAsync<T>(this Uri url,
                                                              JsonSerializerSettings settings,
                                                              bool checkNetAvailability,
                                                              CancellationToken cancellationToken)
    {
        T res = default;

        if (!checkNetAvailability
            || await FExUriExtensions.CheckForInternetConnectionAsync(null))
        {
            if (url.Scheme is "http" or "https")
            {
                using var client = new HttpClient();
                using var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
#if NETSTANDARD2_0
                using var jsonStream = await response.Content.ReadAsStreamAsync();
#elif NETSTANDARD2_1
                await using var jsonStream = await response.Content.ReadAsStreamAsync();
#else
                await using var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);
#endif
                if (jsonStream is not null)
                    res = jsonStream.DeserializeFromStream<T>(settings);
            }
            else
            {
                using var response = await url.GetUriResponseAsync();
#if NETSTANDARD2_0
                using var jsonStream = response.GetResponseStream();
#else
                await using var jsonStream = response.GetResponseStream();
#endif
                if (jsonStream is not null)
                    res = jsonStream.DeserializeFromStream<T>(settings);
            }
        }

        return res;
    }
}