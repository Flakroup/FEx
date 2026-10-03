using FEx.Agnostics.Abstractions.Models;
using System.Net;

namespace FEx.Agnostics.Abstractions.Extensions.Web;

public static class WebRequestExtensions
{
    public static void PrepareRequest(this WebRequest req, WebRequestParams pars)
    {
        if (pars.Credentials is not null)
            req.Credentials = pars.Credentials;

        if (pars.Headers is not null)
            foreach (var header in pars.Headers)
                req.Headers.Add(header.Key, header.Value);

        if (pars.Method is not null)
            req.Method = pars.Method;

        if (pars.Timeout is not null)
            req.Timeout = pars.Timeout.Value;
    }
}