using FEx.Extensions.Web;
using FEx.Json;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Webx;

public static class JsonExtensions
{
    public static async Task<T> DeserializeRemoteJsonAsync<T>(this Uri url, JsonSerializerSettings settings = null, bool checkNetAvailability = false, CancellationToken cancellationToken = default)
    {
        T res = default;

        if (!checkNetAvailability
            || await UriExtensions.CheckForInternetConnectionAsync(null))
        {
            if (url.Scheme == "http"
                || url.Scheme == "https")
            {
                using var client = new HttpClient();
                using HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
                using HttpResponseMessage ensuredResponse = response.EnsureSuccessStatusCode();
                await using Stream jsonStream = await ensuredResponse.Content.ReadAsStreamAsync(cancellationToken);
                if (jsonStream != null)
                    res = jsonStream.DeserializeFromStream<T>(settings);
            }
            else
            {
                using WebResponse response = await url.GetUriResponseAsync();
                await using Stream jsonStream = response.GetResponseStream();
                if (jsonStream != null)
                    res = jsonStream.DeserializeFromStream<T>(settings);
            }
        }

        return res;
    }
}