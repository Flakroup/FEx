using FEx.Agnostics.Abstractions.Models;
using System.Net;

namespace FEx.Agnostics.Abstractions.Extensions.Web;

/// <summary>Extensions for legacy <see cref="System.Net.WebRequest" /> instances.</summary>
public static class WebRequestExtensions
{
    /// <summary>Applies the credentials, headers, method and timeout of the parameters to a request; unset values are skipped.</summary>
    /// <param name="req">The request to configure.</param>
    /// <param name="pars">The parameters to apply.</param>
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