using FEx.Abstractions;
using FEx.Extensions.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Extensions.Web;

public static class UriExtensions
{
    public static async Task<WebResponse> GetWebResponseAsync(this Uri url, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        if (url.Scheme == "http"
            || url.Scheme == "https")
            return await url.GetUriHttpResponseAsync(pars, stopwatch);

        if (url.Scheme == "file")
            return await url.GetUriFileResponseAsync(pars, stopwatch);

        return await url.GetUriResponseAsync(pars, stopwatch);
    }

    public static async Task<long> GetHttpFileSizeAsync(this Uri url, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        return await url.DoHttpResponseFuncAsync((response, _) => response.ContentLength, pars, stopwatch);
    }

    public static async Task<bool> CheckForInternetConnectionAsync(this Uri url)
    {
        if (url is null)
            url = new("http://clients3.google.com/generate_204");

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

    public static async Task<Dictionary<string, string>> GetResponseHeadersAsync(this Uri url, WebRequestParams pars = null)
    {
        return await DoHttpResponseFuncAsync(url, (response, _) => response.GetAllHeaders(), pars);
    }

    public static async Task<T> DoHttpResponseFuncTaskAsync<T>(this Uri url, Func<HttpWebResponse, HttpWebRequest, Task<T>> func, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        if (SynchronizationContext.Current is not null)
            return await Task.Run(() => InternalDoHttpResponseFuncTaskAsync(url, func, pars, stopwatch));

        return await InternalDoHttpResponseFuncTaskAsync(url, func, pars, stopwatch);
    }

    public static async Task<T> DoHttpResponseFuncAsync<T>(this Uri url, Func<HttpWebResponse, HttpWebRequest, T> func, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        if (SynchronizationContext.Current is not null)
            return await Task.Run(() => InternalDoHttpResponseFuncAsync(url, func, pars, stopwatch));

        return await InternalDoHttpResponseFuncAsync(url, func, pars, stopwatch);
    }

    public static async Task DoHttpResponseActionAsync(this Uri url, Action<HttpWebResponse, HttpWebRequest> action, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        if (SynchronizationContext.Current is not null)
            await Task.Run(() => InternalDoHttpResponseActionAsync(url, action, pars, stopwatch));
        else
            await InternalDoHttpResponseActionAsync(url, action, pars, stopwatch);
    }

    public static async Task<T> DoHttpClientResponseFuncTaskAsync<T>(this Uri url, Func<HttpResponseMessage, HttpClient, Task<T>> func, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        if (SynchronizationContext.Current is not null)
            return await Task.Run(() => InternalDoHttpClientResponseFuncTaskAsync(url, func, pars, stopwatch));

        return await InternalDoHttpClientResponseFuncTaskAsync(url, func, pars, stopwatch);
    }

    public static async Task<T> DoHttpClientResponseFuncAsync<T>(this Uri url, Func<HttpResponseMessage, HttpClient, T> func, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        if (SynchronizationContext.Current is not null)
            return await Task.Run(() => InternalDoHttpClientResponseFuncAsync(url, func, pars, stopwatch));

        return await InternalDoHttpClientResponseFuncAsync(url, func, pars, stopwatch);
    }

    public static async Task DoHttpClientResponseActionAsync(this Uri url, Action<HttpResponseMessage, HttpClient> action, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        if (SynchronizationContext.Current is not null)
            await Task.Run(() => InternalDoHttpClientResponseActionAsync(url, action, pars, stopwatch));
        else
            await InternalDoHttpClientResponseActionAsync(url, action, pars, stopwatch);
    }

    public static async Task<FileWebResponse> GetUriFileResponseAsync(this Uri url, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        return (FileWebResponse)await url.GetUriResponseAsync(pars, stopwatch);
    }

    public static async Task<HttpWebResponse> GetUriHttpResponseAsync(this Uri url, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        HttpWebRequest req = url.GetHttpRequest(pars);
        stopwatch?.Restart();
        var response = (HttpWebResponse)await req.GetResponseAsync();
        stopwatch?.Stop();
        return response;
    }

    public static async Task<WebResponse> GetUriResponseAsync(this Uri url, WebRequestParams pars = null, Stopwatch stopwatch = null)
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

    public static async Task<(bool, LengthType)> TryGetRangeAsync(this Uri url, int rangeFrom, int rangeTo, IExceptionHandler exceptionHandler = null, WebRequestParams pars = null)
    {
        using WebResponse resp = await url.GetUriResponseAsync(pars);
        return await resp.TryGetRangeAsync(rangeFrom, rangeTo, exceptionHandler, pars);
    }

    public static async Task<bool> CheckIfLinkIsExpiredAsync(this Uri link, WebRequestParams pars = null)
    {
        if (link is not null)
            try
            {
                return await link.DoHttpResponseFuncAsync((response, _) => response.ContentLength <= 0, pars);
            }
            catch
            {
                return true;
            }

        return true;
    }

    public static Uri TryGetUri(this string uri)
    {
        if (uri.IsNotNullOrEmptyString()
            && Uri.TryCreate(uri, UriKind.Absolute, out Uri uriResult)
            && uriResult is not null)
            return uriResult;

        return null;
    }

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
            if (statusCode >= 100
                && statusCode < 400) //Good requests
                return true;

            if (statusCode >= 500
                && statusCode <= 510) //Server Errors
            {
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

    private static async Task<T> InternalDoHttpResponseFuncTaskAsync<T>(Uri url, Func<HttpWebResponse, HttpWebRequest, Task<T>> func, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        HttpWebRequest req = url.GetHttpRequest(pars);
        stopwatch?.Restart();

        using WebResponse response = await req.GetResponseAsync();
        stopwatch?.Stop();
        using var resp = (HttpWebResponse)response;
        return await func(resp, req);
    }

    private static async Task<T> InternalDoHttpResponseFuncAsync<T>(Uri url, Func<HttpWebResponse, HttpWebRequest, T> func, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        HttpWebRequest req = url.GetHttpRequest(pars);
        stopwatch?.Restart();

        using WebResponse response = await req.GetResponseAsync();
        stopwatch?.Stop();
        using var resp = (HttpWebResponse)response;
        return func(resp, req);
    }

    private static async Task InternalDoHttpResponseActionAsync(Uri url, Action<HttpWebResponse, HttpWebRequest> action, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        HttpWebRequest req = url.GetHttpRequest(pars);
        stopwatch?.Restart();

        using WebResponse response = await req.GetResponseAsync();
        stopwatch?.Stop();
        using var resp = (HttpWebResponse)response;
        action(resp, req);
    }

    private static async Task<T> InternalDoHttpClientResponseFuncTaskAsync<T>(this Uri url, Func<HttpResponseMessage, HttpClient, Task<T>> func, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        WebClientExtensions.PrepareHttpClient(out HttpClient client, pars);
        using (client)
        {
            stopwatch?.Restart();
            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                stopwatch?.Stop();
                using (HttpResponseMessage ensuredResponse = response.EnsureSuccessStatusCode())
                    return await func(ensuredResponse, client);
            }
        }
    }

    private static async Task<T> InternalDoHttpClientResponseFuncAsync<T>(this Uri url, Func<HttpResponseMessage, HttpClient, T> func, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        WebClientExtensions.PrepareHttpClient(out HttpClient client, pars);
        using (client)
        {
            stopwatch?.Restart();
            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                stopwatch?.Stop();
                using (HttpResponseMessage ensuredResponse = response.EnsureSuccessStatusCode())
                    return func(ensuredResponse, client);
            }
        }
    }

    private static async Task InternalDoHttpClientResponseActionAsync(this Uri url, Action<HttpResponseMessage, HttpClient> action, WebRequestParams pars = null, Stopwatch stopwatch = null)
    {
        WebClientExtensions.PrepareHttpClient(out HttpClient client, pars);
        using (client)
        {
            stopwatch?.Restart();
            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                stopwatch?.Stop();
                using (HttpResponseMessage ensuredResponse = response.EnsureSuccessStatusCode())
                    action(ensuredResponse, client);
            }
        }
    }
}