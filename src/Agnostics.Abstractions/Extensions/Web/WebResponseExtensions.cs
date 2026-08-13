using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Extensions.Web;

public static class WebResponseExtensions
{
    public const string ContentRangeHeaderName = "Content-Range";
    public const string AcceptRangesHeaderName = "Accept-Ranges";

    public static Dictionary<string, string> GetAllHeaders(this WebResponse resp) =>
        // Keys come from AllKeys, so the indexer never returns null for them.
        resp.Headers.AllKeys.ToDictionary(x => x, x => resp.Headers[x]!);

    public static async Task<(bool, LengthType)> TryGetRangeAsync(this WebResponse response,
                                                                  int rangeFrom,
                                                                  int rangeTo,
                                                                  WebRequestParams? pars = null) =>
        await response.ResponseUri.TryGetRangeAsync(response.GetAllHeaders(), rangeFrom, rangeTo, pars);

    public static async Task<(bool, LengthType)> TryGetRangeAsync(this Uri responseUri,
                                                                  Dictionary<string, string> responseHeaders,
                                                                  int rangeFrom,
                                                                  int rangeTo,
                                                                  WebRequestParams? pars = null)
    {
        if (!responseHeaders.ContainsKey(AcceptRangesHeaderName))
            return (false, LengthType.AutoDetect);

        var myHttpWebRequest = responseUri.GetHttpRequest(pars);
        myHttpWebRequest.AddRange(rangeFrom, rangeTo);

        using var res = await myHttpWebRequest.GetResponseAsync();
        using var resp = (HttpWebResponse)res;
        responseHeaders = resp.GetAllHeaders();

        return (responseHeaders.ContainsKey(ContentRangeHeaderName), LengthType.Bytes);
    }

    public static ContentRangeHeaderValue? GetContentRange(this HttpWebResponse response)
    {
        var resultHeaders = response.GetAllHeaders();
        var rangeHeader = resultHeaders.TryGetKeyValue<string, string>(ContentRangeHeaderName);

        return rangeHeader.GetContentRange();
    }

    public static ContentRangeHeaderValue? GetContentRange(this string? rangeHeader)
    {
        if (rangeHeader?.Trim().IsNullOrEmptyString() ?? true)
            return null;

        var split = rangeHeader.Split(' ')[1].Split('/')[0].Split('-');
        var from = long.Parse(split[0]);
        var to = long.Parse(split[1]);

        return new(from, to);
    }

    public static Dictionary<string, string[]> GetAllHeaders(this HttpResponseMessage resp)
    {
        KeyValuePair<string, IEnumerable<string>>[] headers = [.. resp.Headers];

        return headers.ToDictionary(x => x.Key, x => x.Value.ToArray());
    }
}