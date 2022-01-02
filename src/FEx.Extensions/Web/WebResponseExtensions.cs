using FEx.Abstractions;
using FEx.Extensions.Collections.Dictionaries;
using FEx.Extensions.Helpers;
using System.Net;
using System.Net.Http.Headers;

namespace FEx.Extensions.Web;

public static class WebResponseExtensions
{
    public static string ContentRangeHeaderName { get; } = "Content-Range";

    public static Dictionary<string, string> GetAllHeaders(this WebResponse resp)
    {
        return resp?.Headers.AllKeys.ToDictionary(x => x, x => resp.Headers[x]);
    }

    public static async Task<(bool, LengthType)> TryGetRangeAsync(this WebResponse response, int rangeFrom, int rangeTo, IExceptionHandler exceptionHandler = null, WebRequestParams pars = null)
    {
        return await TryGetRangeAsync(response.ResponseUri, response.GetAllHeaders(), rangeFrom, rangeTo, exceptionHandler, pars);
    }

    public static async Task<(bool, LengthType)> TryGetRangeAsync(this Uri responseUri, Dictionary<string, string> responseHeaders, int rangeFrom, int rangeTo, IExceptionHandler exceptionHandler = null, WebRequestParams pars = null)
    {
        try
        {
            if (responseHeaders.ContainsKey("Accept-Ranges"))
            {
                HttpWebRequest myHttpWebRequest = responseUri.GetHttpRequest(pars);
                myHttpWebRequest.AddRange(rangeFrom, rangeTo);

                using (WebResponse res = await myHttpWebRequest.GetResponseAsync())
                using (var resp = (HttpWebResponse)res)
                {
                    responseHeaders = resp.GetAllHeaders();
                    return (responseHeaders.ContainsKey(ContentRangeHeaderName), LengthType.Bytes);
                }
            }
        }
        catch (Exception ex) when (exceptionHandler is not null)
        {
            exceptionHandler.Handle(ex);
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
        {
            return null;
        }

        string[] split = rangeHeader.Split(' ')[1].Split('/')[0].Split('-');
        long from = long.Parse(split[0]);
        long to = long.Parse(split[1]);
        return new ContentRangeHeaderValue(from, to);
    }

    public static Dictionary<string, string[]> GetAllHeaders(this HttpResponseMessage resp)
    {
        KeyValuePair<string, IEnumerable<string>>[] headers = resp.Headers.ToArray();

        return headers.ToDictionary(x => x.Key, x => x.Value.ToArray());
    }
}