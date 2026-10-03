using FEx.Agnostics.Abstractions.Models;
using System;
using System.Net.Http;

namespace FEx.Agnostics.Abstractions.Extensions.Web;

public static class WebRequestParamsExtensions
{
    public static HttpClientHandler GetHttpClientHandler(this WebRequestParams pars)
    {
        var handler = new HttpClientHandler();

        if (pars is not null)
        {
            if (pars.Credentials is not null)
                handler.Credentials = pars.Credentials;

            if (pars.Cookies is not null)
                handler.CookieContainer = pars.Cookies;

            if (pars.Proxy is not null)
                handler.Proxy = pars.Proxy;

            // HttpWebRequest.Proxy = null meant "no proxy"; on a handler that is UseProxy = false (a null Proxy would
            // still fall back to the system proxy).
            if (pars.IsProxyNull)
            {
                handler.Proxy = null;
                handler.UseProxy = false;
            }

            if (pars.ServerCertificateValidationCallback is { } validate)
#if NETSTANDARD2_0
                throw new PlatformNotSupportedException(
                    "ServerCertificateValidationCallback is not supported by HttpClientHandler on netstandard2.0.");
#else
                handler.ServerCertificateCustomValidationCallback =
                    (message, cert, chain, errors) => validate(message, cert, chain, errors);
#endif
        }

        // Client certificates stay Manual: nothing in WebRequestParams carries one, so none may be offered to a server.

        return handler;
    }

    public static WebRequestParams Merge(this WebRequestParams pars, WebRequestParams other)
    {
        if (pars is not null)
        {
            if (other.Credentials is not null)
                pars.Credentials = other.Credentials;

            if (other.Cookies is not null)
                pars.Cookies = other.Cookies;

            if (other.Proxy is not null)
                pars.Proxy = other.Proxy;

            if (other.IsProxyNull)
                pars.Proxy = null;

            if (other.Headers is not null)
                pars.Headers = other.Headers;

            if (other.UserAgent is not null)
                pars.UserAgent = other.UserAgent;

            if (other.Method is not null)
                pars.Method = other.Method;

            if (other.Timeout is not null)
                pars.Timeout = other.Timeout.Value;

            if (other.Pipelined.HasValue)
                pars.Pipelined = other.Pipelined.Value;

            if (other.KeepAlive.HasValue)
                pars.KeepAlive = other.KeepAlive.Value;

            if (other.ReadWriteTimeout.HasValue)
                pars.ReadWriteTimeout = other.ReadWriteTimeout.Value;
        }
        else
        {
            pars = other;
        }

        return pars;
    }
}