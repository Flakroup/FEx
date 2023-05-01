using FEx.Abstractions;
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
        if (url is null)
            url = new("http://clients3.google.com/generate_204");

        try
        {
            var request = WebRequest.Create(url);
            using WebResponse response = await request.GetResponseAsync();
            return true;
        }
        catch (Exception ex)
        {
            _exceptionHandler.Handle(ex);
            return false;
        }
    }
}