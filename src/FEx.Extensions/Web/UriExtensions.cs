using FEx.Extensions.Base.Enums;
using FEx.Extensions.Base.Models;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace FEx.Extensions.Web;

public static class UriExtensions
{
    public static async Task<WebResponse> GetWebResponseAsync(this Uri url,
                                                              WebRequestParams pars = null,
                                                              Stopwatch stopwatch = null)
    {
        if (url.Scheme is "http" or "https")
            return await url.GetUriHttpResponseAsync(pars, stopwatch);

        return url.Scheme == "file"
            ? await url.GetUriFileResponseAsync(pars, stopwatch)
            : await url.GetUriResponseAsync(pars, stopwatch);
    }

    public static async Task<bool> CheckForInternetConnectionAsync(this Uri url)
    {
        url ??= new Uri("http://clients3.google.com/generate_204");

        try
        {
            var request = WebRequest.Create(url);
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
        HttpWebRequest myWebRequest = WebRequest.CreateHttp(url);
        if (pars is not null)
            myWebRequest.PrepareRequest(pars);

        return myWebRequest;
    }

    public static WebRequest GetWebRequest(this Uri url, WebRequestParams pars = null)
    {
        var myWebRequest = WebRequest.Create(url);
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
        uri.IsNotNullOrEmptyString() && Uri.TryCreate(uri, UriKind.Absolute, out Uri uriResult) && uriResult is not null
            ? uriResult
            : null;

    public static async Task<bool> UrlIsValidAsync(this Uri url, WebRequestParams pars = null)
    {
        try
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);

            if (pars is not null)
                request.PrepareRequest(pars);

            request.Method = "HEAD"; //Get only the header information -- no need to download any content

            using WebResponse response = await request.GetResponseAsync();
            using var httpResponse = (HttpWebResponse)response;
            var statusCode = (int)httpResponse.StatusCode;
            switch (statusCode)
            {
                //Good requests
                case >= 100 and < 400:
                    return true;
                //Server Errors
                case >= 500 and <= 510:
                    Debug.WriteLine($"The remote server has thrown an internal error. Url is not valid: {url}");
                    return false;
            }
        }
        catch (WebException ex)
        {
            if (ex.Status == WebExceptionStatus.ProtocolError) //400 errors
                return false;

            Debug.WriteLine($"Unhandled status [{ex.Status}] returned for url: {url}", ex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not test url {url}.", ex); //todo logger
        }

        return false;
    }
}