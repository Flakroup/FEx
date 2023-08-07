using System.Net.Http;
using FEx.Extensions.Base.Models;

namespace FEx.Extensions.Web;

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

            if (pars.IsProxyNull)
                handler.Proxy = null;
        }

        handler.ClientCertificateOptions = ClientCertificateOption.Automatic;

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