using FEx.Extensions.Base.Enums;
using FEx.Extensions.Base.Models;
using FEx.Extensions.Collections.Dictionaries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FEx.Extensions.Web;

public static class WebResponseExtensions
{
    public const string ContentRangeHeaderName = "Content-Range";
    public const string AcceptRangesHeaderName = "Accept-Ranges";

    public static Dictionary<string, string> GetAllHeaders(this WebResponse resp)
    {
        return resp?.Headers.AllKeys.ToDictionary(x => x, x => resp.Headers[x]);
    }

    public static async Task<(bool, LengthType)> TryGetRangeAsync(this WebResponse response,
                                                                  int rangeFrom,
                                                                  int rangeTo,
                                                                  WebRequestParams pars = null) =>
        await TryGetRangeAsync(response.ResponseUri, response.GetAllHeaders(), rangeFrom, rangeTo, pars);

    public static async Task<(bool, LengthType)> TryGetRangeAsync(this Uri responseUri,
                                                                  Dictionary<string, string> responseHeaders,
                                                                  int rangeFrom,
                                                                  int rangeTo,
                                                                  WebRequestParams pars = null)
    {
        try
        {
            if (responseHeaders.ContainsKey(AcceptRangesHeaderName))
            {
                HttpWebRequest myHttpWebRequest = responseUri.GetHttpRequest(pars);
                myHttpWebRequest.AddRange(rangeFrom, rangeTo);

                using WebResponse res = await myHttpWebRequest.GetResponseAsync();
                using var resp = (HttpWebResponse)res;
                responseHeaders = resp.GetAllHeaders();

                return (responseHeaders.ContainsKey(ContentRangeHeaderName), LengthType.Bytes);
            }
        }
        catch (Exception ex)
        {
            ex.HandleException();
        }

        return (false, LengthType.AutoDetect);
    }

    public static ContentRangeHeaderValue GetContentRange(this HttpWebResponse response)
    {
        Dictionary<string, string> resultHeaders = response.GetAllHeaders();

        return GetContentRange(resultHeaders.TryGetKeyValue(ContentRangeHeaderName));
    }

    public static ContentRangeHeaderValue GetContentRange(this string rangeHeader)
    {
        if (rangeHeader?.Trim()?.IsNullOrEmptyString() ?? true)
            return null;

        string[] split = rangeHeader.Split(' ')[1].Split('/')[0].Split('-');
        var from = long.Parse(split[0]);
        var to = long.Parse(split[1]);

        return new ContentRangeHeaderValue(from, to);
    }

    public static Dictionary<string, string[]> GetAllHeaders(this HttpResponseMessage resp)
    {
        KeyValuePair<string, IEnumerable<string>>[] headers = resp.Headers.ToArray();

        return headers.ToDictionary(x => x.Key, x => x.Value.ToArray());
    }
}