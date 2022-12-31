using System.Net.Http;

namespace FEx.Extensions.Web;

public static class WebRequestParamsExtensions
{
    public static HttpClientHandler GetHttpClientHandler(this WebRequestParams pars)
    {
        var handler = new HttpClientHandler();
        if (pars != null)
        {
            if (pars.Credentials != null)
                handler.Credentials = pars.Credentials;

            if (pars.Cookies != null)
                handler.CookieContainer = pars.Cookies;

            if (pars.Proxy != null)
                handler.Proxy = pars.Proxy;

            if (pars.IsProxyNull)
                handler.Proxy = null;
        }

        handler.ClientCertificateOptions = ClientCertificateOption.Automatic;

        return handler;
    }

    public static WebRequestParams Merge(this WebRequestParams pars, WebRequestParams other)
    {
        if (pars != null)
        {
            if (other.Credentials != null)
                pars.Credentials = other.Credentials;

            if (other.Cookies != null)
                pars.Cookies = other.Cookies;

            if (other.Proxy != null)
                pars.Proxy = other.Proxy;

            if (other.IsProxyNull)
                pars.Proxy = null;

            if (other.Headers != null)
                pars.Headers = other.Headers;

            if (other.UserAgent != null)
                pars.UserAgent = other.UserAgent;

            if (other.Method != null)
                pars.Method = other.Method;

            if (other.Timeout != null)
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