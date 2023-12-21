using FEx.Basics.Flow;
using System.Net;

namespace FEx.Webx.Flow;

public class ApiError : StackError<HttpStatusCode?>
{
    public string RequestUrl { get; }

    public ApiError()
        : base(null)
    {
    }

    public ApiError(string requestUrl = null, HttpStatusCode? status = null)
        : base(status)
    {
        RequestUrl = requestUrl;
    }

    public ApiError(string requestUrl, HttpStatusCode? status, string message = null)
        : base(status, message)
    {
        RequestUrl = requestUrl;
    }

    public ApiError(string requestUrl, HttpStatusCode? status, Error innerError, string message = null)
        : base(status, innerError, message)
    {
        RequestUrl = requestUrl;
    }
}