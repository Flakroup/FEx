using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.Abstractions.Models;
using FEx.Core.Abstractions.Extensions;
using FEx.MVVM;
using FEx.MVVM.Abstractions.Enums;
using FEx.Webx.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace FEx.Legacy.Web;

public static class WebServices
{
    public static string BaseRequestUrl { get; set; } = string.Empty;
    public static Dictionary<int, string> StatusCodes { get; set; } = [];

    /// <summary>
    /// Handles GET requests.
    /// </summary>
    /// <param name="requestUrl">Pass only what's after <see cref="BaseRequestUrl" /></param>
    /// <param name="credentials">The credentials.</param>
    /// <param name="resultAsJson">Set to true (which is default) if you expect JSON in response. Else set to false.</param>
    /// <param name="omitCodes">List of expected - not critical status codes.</param>
    /// <param name="cookies">The cookies.</param>
    /// <returns>
    /// <see cref="string" /> with requested data.
    /// </returns>
    public static async Task<string?> GetRequestResultAsync(string requestUrl,
                                                           ICredentials? credentials = null,
                                                           bool resultAsJson = true,
                                                           List<HttpStatusCode>? omitCodes = null,
                                                           List<Cookie>? cookies = null)
    {
        string? responseBody = null;
        requestUrl = $"{BaseRequestUrl}{requestUrl}";

        //requestUrl = Uri.EscapeUriString(requestUrl);
        if (requestUrl.IsNotNullOrEmptyString())
        {
            using var client = PrepareHttpClient(requestUrl, credentials, resultAsJson, cookies);
            HttpResponseMessage? response = null;

            try
            {
                // Guard: GetHttpResponseAsync returns null only after all retries throw; original code dereferenced it and would NRE - Guard preserves the throw-on-failure behavior.
#pragma warning disable IDISP004 // Guard returns the same instance; it does not create a disposable
                response = (await GetHttpResponseAsync(() => client.GetAsync(requestUrl))).Guard(nameof(response));
#pragma warning restore IDISP004
                responseBody = await response.Content.ReadAsStringAsync();

                if (omitCodes?.Contains(response.StatusCode) != true)
                {
                    response.EnsureSuccessStatusCode();

                    if (requestUrl.EndsWith(".xml") && resultAsJson) //todo detect better xml data
                        responseBody = ConvertXmlToJson(responseBody);
                }
                else
                {
                    await FExMvvm.MessagePopupService.ShowMessageAsync(
                        $"API response: {GetWebApiResponseCodeInfo(response.StatusCode)}\n\nRequest URL: {requestUrl}\n\nReceived response: {responseBody}\n",
                        "Something wrong happened",
                        MessageIcon.Exclamation,
                        FExMessageButton.OK,
                        null,
                        true,
                        false,
                        null,
                        LogLevel.Information,
                        null);

                    return null;
                }
            }
            catch (Exception ex)
            {
                var webApiResp = string.Empty;

                if (response is not null)
                    webApiResp = GetWebApiResponseCodeInfo(response.StatusCode);

                ex.HandleException(custom: ("additionalInfo",
                    $"API response: {webApiResp}\n\nRequest URL: {requestUrl}\n\nReceived response: {responseBody}\n"));

                responseBody = null;
            }
            finally
            {
                response?.Dispose();
            }
        }

        return responseBody;
    }

    public static async Task<HttpResponseMessage?> GetHttpResponseAsync(Func<Task<HttpResponseMessage>> responseFunc)
    {
        HttpResponseMessage? response = null;
        var retry = 20;

        while (retry > 0)
        {
            try
            {
#pragma warning disable IDISP003 // retry loop, response is null on re-entry after exception
                response = await responseFunc();
#pragma warning restore IDISP003
                retry = 0;
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException)
                {
                    retry = 0;
                    ex.HandleException(false);
                }
                else
                {
                    ex.HandleException();
                }
            }

            retry--;
        }

        return response;
    }

    /// <summary>
    /// Converts the XML to json.
    /// </summary>
    /// <param name="responseBody">The response body.</param>
    /// <returns></returns>
    public static string ConvertXmlToJson(string responseBody)
    {
        var doc = new XmlDocument();
        doc.LoadXml(responseBody);

        foreach (XmlNode node in doc)
        {
            if (node.NodeType == XmlNodeType.XmlDeclaration)
                doc.RemoveChild(node);
        }

        return JsonConvert.SerializeXmlNode(doc, Formatting.Indented);
    }

    /// <summary>
    /// Prepares the HTTP client.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="credentials">The credentials.</param>
    /// <param name="resultAsJson">if set to <c>true</c> [result as json].</param>
    /// <param name="cookies">The cookies.</param>
    /// <returns></returns>
    // ReSharper disable UnusedParameter.Global
    public static HttpClient PrepareHttpClient(string address,
                                               ICredentials? credentials,
                                               bool resultAsJson,
                                               IEnumerable<Cookie>? cookies = null)
        // ReSharper restore UnusedParameter.Global
    {
        var pars = new WebRequestParams(cookies)
        {
            Credentials = credentials,
            Timeout = 100 * 1000,
            Headers = new Dictionary<string, string>
            {
                ["User-Agent"] = "VSTSAuthSample-AuthenticateADALNonInteractive", //todo move to TFS
                ["X-TFS-FedAuthRedirect"] = "Suppress"
            }
        };

        WebClientExtensions.PrepareHttpClient(out var client, pars, resultAsJson);

        return client;
    }

    //public static void SetDefaultProxy(this HttpClientHandler httpClientHandler, Uri address, bool useDefaultCredentials = true, ICredentials credentials = null)
    //{
    //    httpClientHandler.Proxy = address.GetDefaultWebProxy(useDefaultCredentials, credentials);
    //}

    //public static IWebProxy GetDefaultWebProxy(this Uri requestedAddress, bool useDefaultCredentials = true, ICredentials credentials = null, bool bypassProxyOnLocal = true)
    //{
    //    IWebProxy defWebProxy = WebRequest.DefaultWebProxy;
    //    if (defWebProxy is not null)
    //    {
    //        Uri proxyUri = defWebProxy.GetProxy(requestedAddress);
    //        if (proxyUri is not null)
    //        {
    //            return new WebProxy(proxyUri)
    //            {
    //                Credentials = useDefaultCredentials ? CredentialCache.DefaultNetworkCredentials : (credentials is not null ? credentials : WebRequest.DefaultWebProxy?.Credentials),
    //                UseDefaultCredentials = useDefaultCredentials,
    //                BypassProxyOnLocal = bypassProxyOnLocal
    //            };
    //        }
    //    }

    //    return WebRequest.DefaultWebProxy;
    //}

    //public static void SetDefaultProxy(this HttpClientHandler httpClientHandler, string address, bool useDefaultCredentials = true, ICredentials credentials = null)
    //{
    //    httpClientHandler.SetDefaultProxy(new Uri(address), useDefaultCredentials, credentials);
    //}

    //public static void SetDefaultProxy(this HttpClientHandler httpClientHandler, Uri address, string userName, string password)
    //{
    //    httpClientHandler.SetDefaultProxy(address, false, new NetworkCredential(userName, password));
    //}

    /// <summary>
    /// Handles POST requests.
    /// </summary>
    /// <param name="requestUrl">Pass only what's after <see cref="BaseRequestUrl" /></param>
    /// <param name="postContent">Serialized JSON object to send in POST request.</param>
    /// <param name="credentials"></param>
    /// <param name="omitCodes">List of expected - not critical status codes.</param>
    /// <param name="cookies"></param>
    /// <returns><see cref="bool" /> indicating success of operation.</returns>
    public static async Task<ResponseResult> PostRequestResultAsync(string requestUrl,
                                                                    string postContent,
                                                                    ICredentials? credentials = null,
                                                                    List<HttpStatusCode>? omitCodes = null,
                                                                    List<Cookie>? cookies = null)
    {
        // SYSLIB0013: Uri.EscapeUriString escapes a complete URI string; switching to
        // Uri.EscapeDataString here would corrupt the scheme/host/slashes. Behavior retained.
#pragma warning disable SYSLIB0013
        requestUrl = Uri.EscapeUriString(BaseRequestUrl + requestUrl);
#pragma warning restore SYSLIB0013
        using var client = PrepareHttpClient(requestUrl, credentials, false, cookies);

        using HttpContent post = new StringContent(postContent,
            Encoding.UTF8,
            MediaTypes.ApplicationJson.GetEnumValueDescription());

        return await HandleResponseAsync(requestUrl, omitCodes, () => client.PostAsync(requestUrl, post));
    }

    /// <summary>
    /// Handles the response.
    /// </summary>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="omitCodes">The omit codes.</param>
    /// <param name="responseHandler">The response handler.</param>
    /// <returns>
    /// System.Boolean
    /// </returns>
    public static async Task<ResponseResult> HandleResponseAsync(string requestUrl,
                                                                 List<HttpStatusCode>? omitCodes,
                                                                 Func<Task<HttpResponseMessage>> responseHandler)
    {
        HttpResponseMessage? response = null;
        string? responseBody = null;
        var isSuccess = false;

        try
        {
            // Guard: GetHttpResponseAsync returns null only after all retries throw; original code dereferenced it and would NRE - Guard preserves the throw-on-failure behavior.
#pragma warning disable IDISP004 // Guard returns the same instance; it does not create a disposable
            response = (await GetHttpResponseAsync(responseHandler)).Guard(nameof(response));
#pragma warning restore IDISP004

            if (omitCodes?.Contains(response.StatusCode) != true)
            {
                responseBody = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();
            }

            isSuccess = true;
        }
        catch (Exception ex)
        {
            var webApiResp = string.Empty;

            if (response is not null)
                webApiResp = GetWebApiResponseCodeInfo(response.StatusCode);

            ex.HandleException(custom: ("additionalInfo",
                $"API response: {webApiResp}\n\nRequest URL: {requestUrl}\n\nReceived response: {responseBody}\n"));
        }
        finally
        {
            response?.Dispose();
        }

        // Coalesce: responseBody is null on failure; ResponseResult.ResponseBody is non-null - empty string represents "no body".
        return new(isSuccess, responseBody ?? string.Empty);
    }

    /// <summary>
    /// Translates <see cref="HttpStatusCode" /> to API status message.
    /// </summary>
    /// <param name="statusCode"><see cref="HttpStatusCode" /> which to translate from.</param>
    /// <returns>Message <see cref="string" /></returns>
    public static string GetWebApiResponseCodeInfo(HttpStatusCode statusCode) =>
        GetWebApiResponseCodeInfo((int)statusCode);

    /// <summary>
    /// Translates <see cref="HttpStatusCode" /> to API status message.
    /// </summary>
    /// <param name="statusCode"><see cref="HttpStatusCode" /> which to translate from.</param>
    /// <returns>Message <see cref="string" /></returns>
    public static string GetWebApiResponseCodeInfo(int statusCode) =>
        StatusCodes.TryGetValue(statusCode, out var code)
            ? code
            : ((HttpStatusCode)statusCode).ToString();
}