using FEx.Extensions.Base.Enums;
using FEx.Extensions.Base.Models;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace FEx.Extensions.Web;

public static class UriExtensions
{
    private const string HttpScheme = "http";
    private const string HttpsScheme = "https";
    private const string FileScheme = "file";
    private static Uri DefaultUri { get; } = new("http://clients3.google.com/generate_204");

    public static async Task<WebResponse> GetWebResponseAsync(this Uri url,
                                                              WebRequestParams pars = null,
                                                              Stopwatch stopwatch = null)
    {
        if (url.Scheme is HttpScheme or HttpsScheme)
            return await url.GetUriHttpResponseAsync(pars, stopwatch);

        return url.Scheme == FileScheme
            ? await url.GetUriFileResponseAsync(pars, stopwatch)
            : await url.GetUriResponseAsync(pars, stopwatch);
    }

    public static async Task<bool> CheckForInternetConnectionAsync(this Uri url)
    {
        url ??= DefaultUri;

        try
        {
#if NET
#pragma warning disable SYSLIB0014
#endif
            var request = WebRequest.Create(url);
#if NET
#pragma warning restore SYSLIB0014
#endif
            using WebResponse _ = await request.GetResponseAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<FileWebResponse> GetUriFileResponseAsync(this Uri url,
                                                                      WebRequestParams pars = null,
                                                                      Stopwatch stopwatch = null) =>
        (FileWebResponse)await url.GetUriResponseAsync(pars, stopwatch);

    public static async Task<HttpWebResponse> GetUriHttpResponseAsync(this Uri url,
                                                                      WebRequestParams pars = null,
                                                                      Stopwatch stopwatch = null)
    {
        HttpWebRequest req = url.GetHttpRequest(pars);
        stopwatch?.Restart();
        var response = (HttpWebResponse)await req.GetResponseAsync();
        stopwatch?.Stop();

        return response;
    }

    public static async Task<WebResponse> GetUriResponseAsync(this Uri url,
                                                              WebRequestParams pars = null,
                                                              Stopwatch stopwatch = null)
    {
        WebRequest req = url.GetWebRequest(pars);
        stopwatch?.Restart();
        WebResponse response = await req.GetResponseAsync();
        stopwatch?.Stop();

        return response;
    }

    public static HttpWebRequest GetHttpRequest(this Uri url, WebRequestParams pars = null)
    {
#if NET
#pragma warning disable SYSLIB0014
#endif
        HttpWebRequest myWebRequest = WebRequest.CreateHttp(url);
#if NET
#pragma warning restore SYSLIB0014
#endif

        if (pars is not null)
            myWebRequest.PrepareRequest(pars);

        return myWebRequest;
    }

    public static WebRequest GetWebRequest(this Uri url, WebRequestParams pars = null)
    {
#if NET
#pragma warning disable SYSLIB0014
#endif
        var myWebRequest = WebRequest.Create(url);
#if NET
#pragma warning restore SYSLIB0014
#endif

        if (pars is not null)
            myWebRequest.PrepareRequest(pars);

        return myWebRequest;
    }

    public static async Task<(bool, LengthType)> TryGetRangeAsync(this Uri url,
                                                                  int rangeFrom,
                                                                  int rangeTo,
                                                                  WebRequestParams pars = null)
    {
        using WebResponse resp = await url.GetUriResponseAsync(pars);

        return await resp.TryGetRangeAsync(rangeFrom, rangeTo, pars);
    }

    public static Uri TryGetUri(this string uri) =>
        uri.IsNotNullOrEmptyString() && Uri.TryCreate(uri, UriKind.Absolute, out Uri uriResult)
#if NETSTANDARD
        && uriResult is not null
#endif
            ? uriResult
            : null;
}