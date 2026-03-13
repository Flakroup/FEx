using FEx.Core.Abstractions.Interfaces;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FEx.Webx;

public class HasInternetConnectionGate
{
    private readonly IExceptionHandler _exceptionHandler;

    public HasInternetConnectionGate(IExceptionHandler exceptionHandler)
    {
        _exceptionHandler = exceptionHandler;
    }

    public async Task<bool> CheckAsync(Uri url = null)
    {
        url ??= new("http://clients3.google.com/generate_204");

        try
        {
#if NET
#pragma warning disable SYSLIB0014
#endif
            var request = WebRequest.Create(url);
#if NET
#pragma warning restore SYSLIB0014
#endif
            using var response = await request.GetResponseAsync();

            return true;
        }
        catch (Exception ex)
        {
            _exceptionHandler.Handle(ex);

            return false;
        }
    }
}