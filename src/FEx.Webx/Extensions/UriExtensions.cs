using FEx.Abstractions.Flow;
using FEx.Abstractions.Flow.Errors;
using FEx.Basics.Extensions;
using FEx.Common.Extensions;
using FEx.Extensions.Base.Models;
using FEx.Extensions.Collections;
using FEx.Extensions.Helpers;
using FEx.Extensions.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

namespace FEx.Webx.Extensions;

public static class UriExtensions
{
    private const string HttpScheme = "http";
    private const string HttpsScheme = "https";
    private const string HeadMethod = "HEAD";
    private const string GetMethod = "GET";
    private const string AdditionalInfoKey = "additionalInfo";
    private const int DefaultTimeout = 100000000;

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
            pars ??= new();

            pars.Method ??= GetMethod;

            pars.Timeout ??= DefaultTimeout;

            var sw = new Stopwatch();

            try
            {
                if (url.Scheme is HttpScheme or HttpsScheme)
                    return await url.DoHttpResponseFuncAsync((response, _) =>
                        {
                            bool result = response?.StatusCode is HttpStatusCode.OK
                                or HttpStatusCode.PartialContent
                                or HttpStatusCode.NonAuthoritativeInformation;

                            return (result, sw.ElapsedMilliseconds);
                        },
                        pars,
                        sw);

                using (WebResponse response = await url.GetUriResponseAsync(pars))
                {
                    bool result = response is not null;

                    return (result, sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                ex.HandleException(custom: (AdditionalInfoKey, url.AbsoluteUri));
            }
        }

        return (false, -1);
    }

    public static async Task<long> GetHttpFileSizeAsync(this Uri url,
                                                        WebRequestParams pars = null,
                                                        Stopwatch stopwatch = null) =>
        await url.DoHttpResponseFuncAsync((response, _) => response.ContentLength, pars, stopwatch);

    public static async Task<Dictionary<string, string>>
        GetResponseHeadersAsync(this Uri url, WebRequestParams pars = null) =>
        await DoHttpResponseFuncAsync(url, (response, _) => response.GetAllHeaders(), pars);

    public static async Task<T> DoHttpResponseFuncTaskAsync<T>(this Uri url,
                                                               Func<HttpWebResponse, HttpWebRequest, Task<T>> func,
                                                               WebRequestParams pars = null,
                                                               Stopwatch stopwatch = null) =>
        await StaticAsyncHelper.ExecuteTaskOnThreadPoolAsync(() =>
            InternalDoHttpResponseFuncTaskAsync(url, func, pars, stopwatch));

    public static async Task<T> DoHttpResponseFuncAsync<T>(this Uri url,
                                                           Func<HttpWebResponse, HttpWebRequest, T> func,
                                                           WebRequestParams pars = null,
                                                           Stopwatch stopwatch = null) =>
        await StaticAsyncHelper.ExecuteTaskOnThreadPoolAsync(() =>
            InternalDoHttpResponseFuncAsync(url, func, pars, stopwatch));

    public static async Task DoHttpResponseActionAsync(this Uri url,
                                                       Action<HttpWebResponse, HttpWebRequest> action,
                                                       WebRequestParams pars = null,
                                                       Stopwatch stopwatch = null) =>
        await StaticAsyncHelper.ExecuteTaskOnThreadPoolAsync(() =>
            InternalDoHttpResponseActionAsync(url, action, pars, stopwatch));

    public static async Task<T> DoHttpClientResponseFuncTaskAsync<T>(this Uri url,
                                                                     Func<HttpResponseMessage, HttpClient, Task<T>>
                                                                         func,
                                                                     WebRequestParams pars = null,
                                                                     Stopwatch stopwatch = null) =>
        await StaticAsyncHelper.ExecuteTaskOnThreadPoolAsync(() =>
            url.InternalDoHttpClientResponseFuncTaskAsync(func, pars, stopwatch));

    public static async Task<T> DoHttpClientResponseFuncAsync<T>(this Uri url,
                                                                 Func<HttpResponseMessage, HttpClient, T> func,
                                                                 WebRequestParams pars = null,
                                                                 Stopwatch stopwatch = null) =>
        await StaticAsyncHelper.ExecuteTaskOnThreadPoolAsync(() =>
            url.InternalDoHttpClientResponseFuncAsync(func, pars, stopwatch));

    public static async Task DoHttpClientResponseActionAsync(this Uri url,
                                                             Action<HttpResponseMessage, HttpClient> action,
                                                             WebRequestParams pars = null,
                                                             Stopwatch stopwatch = null) =>
        await StaticAsyncHelper.ExecuteTaskOnThreadPoolAsync(() =>
            url.InternalDoHttpClientResponseActionAsync(action, pars, stopwatch));

    public static async Task<bool> CheckIfLinkIsExpiredAsync(this Uri link, WebRequestParams pars = null)
    {
        if (link is not null)
            try
            {
                pars ??= new();
                pars.Method = HeadMethod;

                return await link.DoHttpResponseFuncAsync((response, _) => response.ContentLength <= 0, pars);
            }
            catch
            {
                return true;
            }

        return true;
    }

    public static async Task<Result<Error>> UrlIsValidAsync(this Uri url, WebRequestParams pars = null)
    {
        try
        {
#if NET
#pragma warning disable SYSLIB0014
#endif
            HttpWebRequest request = WebRequest.CreateHttp(url);
#if NET
#pragma warning restore SYSLIB0014
#endif

            if (pars is not null)
                request.PrepareRequest(pars);

            request.Method = HeadMethod; //Get only the header information -- no need to download any content

            using WebResponse response = await request.GetResponseAsync();
            using var httpResponse = (HttpWebResponse)response;
            var statusCode = (int)httpResponse.StatusCode;

            switch (statusCode)
            {
                //Good requests
                case >= 100 and < 400:
                    return Result<Error>.Success;
                //Server Errors
                case >= 500 and <= 510:
                    return new StackError($"The remote server has thrown an internal error. Url is not valid: {url}");
            }

            return new StackError($"The remote server has thrown an unexpected code. Url is not valid: {url}");
        }
        catch (WebException ex)
        {
            if (ex.Status == WebExceptionStatus.ProtocolError) //400 errors
                return new ExceptionError(ex, $"Unhandled status [{ex.Status}] returned for url: {url}");
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex, $"Could not test url {url}.");
        }

        return new StackError($"Could not test url {url}.");
    }

    public static async Task<string> GetFileNameAsync(this Uri url, WebRequestParams pars = null) => await url.DoHttpResponseFuncAsync((response, _) => response.GetFileName(), pars);

    public static string GetFileName(this HttpWebResponse response)
    {
        Dictionary<string, string> responseHeaders = response.GetAllHeaders();

        return GetFileName(response.ResponseUri, responseHeaders);
    }

    public static string GetFileName(this HttpResponseMessage response)
    {
        Dictionary<string, string[]> responseHeaders = response.GetAllHeaders();

        return GetFileName(response.RequestMessage!.RequestUri, responseHeaders);
    }

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

    private static string GetFileName(Uri responseUri, IDictionary<string, string> responseHeaders)
    {
        string contentDispositionHeader = responseHeaders.Keys.FindInEnumerable(x => x.IsEqual("content-disposition"));

        return GetFileName(responseUri,
            contentDispositionHeader is not null
                ? responseHeaders[contentDispositionHeader]
                : null,
            responseHeaders.ToDictionary(x => x.Key, x => new[] { x.Value }));
    }

    private static string GetFileName(Uri responseUri, IDictionary<string, string[]> responseHeaders)
    {
        string contentDispositionHeader = responseHeaders.Keys.FindInEnumerable(x => x.IsEqual("content-disposition"));

        return GetFileName(responseUri,
            contentDispositionHeader is not null
                ? responseHeaders[contentDispositionHeader][0]
                : null,
            responseHeaders);
    }

    private static string GetFileName(Uri responseUri,
                                      string contentDispositionHeaderValue,
                                      IDictionary<string, string[]> responseHeaders)
    {
        if (contentDispositionHeaderValue is not null)
        {
            var values = contentDispositionHeaderValue.Split(';')
                .Select(x => x.Split('='))
                .ToDictionary(x => x[0].Trim(),
                    x => x.Length > 1
                        ? x[1]
                        : null);

            string fileNameKey = values.Keys.FirstOrDefault(x => x.IsEqual("filename"));

            if (fileNameKey is not null
                && values.TryGetKeyValue(fileNameKey).IsNotNullOrEmptyString())
            {
                var contentDisposition = new ContentDisposition(contentDispositionHeaderValue);

                if (contentDisposition.FileName is not null)
                    return Uri.UnescapeDataString(contentDisposition.FileName);
            }
        }

        string fName = Uri.UnescapeDataString(responseUri.Segments.Last());
        string ext = Path.GetExtension(fName);

        string mimeType = responseHeaders.Where(x => x.Key.IsEqual("Content-Type"))
            .Select(x => x.Value)
            .SingleOrDefault()
            ?.FirstOrDefault()
            ?.Split(';')[0];

        IReadOnlyCollection<string> exts = null;

        if (mimeType.IsNotNullOrEmptyString())
            exts = MimeTypesUtility.GetDefaultExtensions(mimeType);

        if (exts.IsNotNullOrEmptyReadOnlyCollection()
            && (ext.TrimStart('.').IsNullOrEmptyString() || !exts.Contains(ext.TrimStart('.')) && !exts.Contains(".*")))
            fName = $"{fName}.{exts.FirstOrDefault()}";

        return fName;
    }
}