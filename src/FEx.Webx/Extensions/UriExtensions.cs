using FEx.Extensions;
using FEx.Extensions.Base.Models;
using FEx.Extensions.Web;
using FEx.Fundamentals;
using FEx.Fundamentals.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace FEx.Webx.Extensions;

public static class UriExtensions
{
    private static AsyncHelper AsyncHelper => Foundation.AsyncHelper;

    /// <summary>
    ///     Determines whether the specified URL is reachable.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="pars">The parameters.</param>
    /// <returns>
    ///     <c>true</c> if the specified URL is reachable; otherwise, <c>false</c>.
    /// </returns>
    public static async Task<(bool, long)> IsUriReachableAsync(this string url, WebRequestParams pars = null) =>
        await new Uri(url).IsUriReachableAsync(pars);

    /// <summary>
    ///     Determines whether the specified URL is reachable.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="pars">The parameters.</param>
    /// <returns>
    ///     <c>true</c> if the specified URL is reachable; otherwise, <c>false</c>.
    /// </returns>
    public static async Task<(bool isAvailable, long loadTime)> IsUriReachableAsync(
        this Uri url,
        WebRequestParams pars = null)
    {
        if (url is not null)
        {
            pars ??= new WebRequestParams();

            pars.Method ??= "GET";

            pars.Timeout ??= 100000000;

            var sw = new Stopwatch();
            try
            {
                if (url.Scheme is "http" or "https")
                    return await url.DoHttpResponseFuncAsync((response, _) =>
                    {
                        bool result = response?.StatusCode is HttpStatusCode.OK
                            or HttpStatusCode.PartialContent
                            or HttpStatusCode.NonAuthoritativeInformation;
                        return (result, sw.ElapsedMilliseconds);
                    }, pars, sw);

                using (WebResponse response = await url.GetUriResponseAsync(pars))
                {
                    bool result = response is not null;
                    return (result, sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                ex.HandleException(false, custom: ("additionalInfo", url.AbsoluteUri));
            }
        }

        return (false, -1);
    }

    public static async Task<long> GetHttpFileSizeAsync(this Uri url,
                                                        WebRequestParams pars = null,
                                                        Stopwatch stopwatch = null)
    {
        return await url.DoHttpResponseFuncAsync((response, _) => response.ContentLength, pars, stopwatch);
    }

    public static async Task<Dictionary<string, string>> GetResponseHeadersAsync(
        this Uri url,
        WebRequestParams pars = null)
    {
        return await DoHttpResponseFuncAsync(url, (response, _) => response.GetAllHeaders(), pars);
    }

    public static async Task<T> DoHttpResponseFuncTaskAsync<T>(this Uri url,
                                                               Func<HttpWebResponse, HttpWebRequest, Task<T>> func,
                                                               WebRequestParams pars = null,
                                                               Stopwatch stopwatch = null)
    {
        return await AsyncHelper.ExecuteTaskOnThreadPoolAsync(() =>
            InternalDoHttpResponseFuncTaskAsync(url, func, pars, stopwatch));
    }

    public static async Task<T> DoHttpResponseFuncAsync<T>(this Uri url,
                                                           Func<HttpWebResponse, HttpWebRequest, T> func,
                                                           WebRequestParams pars = null,
                                                           Stopwatch stopwatch = null)
    {
        return await AsyncHelper.ExecuteTaskOnThreadPoolAsync(() =>
            InternalDoHttpResponseFuncAsync(url, func, pars, stopwatch));
    }

    public static async Task DoHttpResponseActionAsync(this Uri url,
                                                       Action<HttpWebResponse, HttpWebRequest> action,
                                                       WebRequestParams pars = null,
                                                       Stopwatch stopwatch = null)
    {
        await AsyncHelper.ExecuteTaskOnThreadPoolAsync(() =>
            InternalDoHttpResponseActionAsync(url, action, pars, stopwatch));
    }

    public static async Task<T> DoHttpClientResponseFuncTaskAsync<T>(this Uri url,
                                                                     Func<HttpResponseMessage, HttpClient, Task<T>>
                                                                         func,
                                                                     WebRequestParams pars = null,
                                                                     Stopwatch stopwatch = null)
    {
        return await AsyncHelper.ExecuteTaskOnThreadPoolAsync(() =>
            url.InternalDoHttpClientResponseFuncTaskAsync(func, pars, stopwatch));
    }

    public static async Task<T> DoHttpClientResponseFuncAsync<T>(this Uri url,
                                                                 Func<HttpResponseMessage, HttpClient, T> func,
                                                                 WebRequestParams pars = null,
                                                                 Stopwatch stopwatch = null)
    {
        return await AsyncHelper.ExecuteTaskOnThreadPoolAsync(() =>
            url.InternalDoHttpClientResponseFuncAsync(func, pars, stopwatch));
    }

    public static async Task DoHttpClientResponseActionAsync(this Uri url,
                                                             Action<HttpResponseMessage, HttpClient> action,
                                                             WebRequestParams pars = null,
                                                             Stopwatch stopwatch = null)
    {
        await AsyncHelper.ExecuteTaskOnThreadPoolAsync(() =>
            url.InternalDoHttpClientResponseActionAsync(action, pars, stopwatch));
    }

    public static async Task<bool> CheckIfLinkIsExpiredAsync(this Uri link, WebRequestParams pars = null)
    {
        if (link is not null)
            try
            {
                pars ??= new WebRequestParams();
                pars.Method = "HEAD";
                return await link.DoHttpResponseFuncAsync((response, _) => response.ContentLength <= 0, pars);
            }
            catch
            {
                return true;
            }

        return true;
    }

    public static Uri TryGetUri(this string uri) =>
        uri.IsNotNullOrEmptyString() && Uri.TryCreate(uri, UriKind.Absolute, out Uri uriResult) && uriResult is not null
            ? uriResult
            : null;

    private static async Task<T> InternalDoHttpResponseFuncTaskAsync<T>(
        Uri url,
        Func<HttpWebResponse, HttpWebRequest, Task<T>> func,
        WebRequestParams pars = null,
        Stopwatch stopwatch = null)
    {
        HttpWebRequest req = url.GetHttpRequest(pars);
        stopwatch?.Restart();

        using WebResponse response = await req.GetResponseAsync();
        stopwatch?.Stop();
        using var resp = (HttpWebResponse)response;
        return await func(resp, req);
    }

    private static async Task<T> InternalDoHttpResponseFuncAsync<T>(Uri url,
                                                                    Func<HttpWebResponse, HttpWebRequest, T> func,
                                                                    WebRequestParams pars = null,
                                                                    Stopwatch stopwatch = null)
    {
        HttpWebRequest req = url.GetHttpRequest(pars);
        stopwatch?.Restart();

        using WebResponse response = await req.GetResponseAsync();
        stopwatch?.Stop();
        using var resp = (HttpWebResponse)response;
        return func(resp, req);
    }

    private static async Task InternalDoHttpResponseActionAsync(Uri url,
                                                                Action<HttpWebResponse, HttpWebRequest> action,
                                                                WebRequestParams pars = null,
                                                                Stopwatch stopwatch = null)
    {
        HttpWebRequest req = url.GetHttpRequest(pars);
        stopwatch?.Restart();

        using WebResponse response = await req.GetResponseAsync();
        stopwatch?.Stop();
        using var resp = (HttpWebResponse)response;
        action(resp, req);
    }

    private static async Task<T> InternalDoHttpClientResponseFuncTaskAsync<T>(
        this Uri url,
        Func<HttpResponseMessage, HttpClient, Task<T>> func,
        WebRequestParams pars = null,
        Stopwatch stopwatch = null)
    {
        WebClientExtensions.PrepareHttpClient(out HttpClient client, pars);
        using (client)
        {
            stopwatch?.Restart();
            using HttpResponseMessage response = await client.GetAsync(url);
            stopwatch?.Stop();
            using HttpResponseMessage ensuredResponse = response.EnsureSuccessStatusCode();
            return await func(ensuredResponse, client);
        }
    }

    private static async Task<T> InternalDoHttpClientResponseFuncAsync<T>(
        this Uri url,
        Func<HttpResponseMessage, HttpClient, T> func,
        WebRequestParams pars = null,
        Stopwatch stopwatch = null)
    {
        WebClientExtensions.PrepareHttpClient(out HttpClient client, pars);
        using (client)
        {
            stopwatch?.Restart();
            using HttpResponseMessage response = await client.GetAsync(url);
            stopwatch?.Stop();
            using HttpResponseMessage ensuredResponse = response.EnsureSuccessStatusCode();
            return func(ensuredResponse, client);
        }
    }

    private static async Task InternalDoHttpClientResponseActionAsync(this Uri url,
                                                                      Action<HttpResponseMessage, HttpClient> action,
                                                                      WebRequestParams pars = null,
                                                                      Stopwatch stopwatch = null)
    {
        WebClientExtensions.PrepareHttpClient(out HttpClient client, pars);
        using (client)
        {
            stopwatch?.Restart();
            using HttpResponseMessage response = await client.GetAsync(url);
            stopwatch?.Stop();
            using HttpResponseMessage ensuredResponse = response.EnsureSuccessStatusCode();
            action(ensuredResponse, client);
        }
    }
}