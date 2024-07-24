using FEx.Abstractions.Extensions;
using FEx.Abstractions.Interfaces;
using System;

namespace FEx.Abstractions.Flow.Errors;

public class ExceptionError : Error, IExceptionError
{
    public Exception Exception { get; }

    public string StackTrace { get; }

    public string RootErrorStackTrace =>
        InnerError.TryGetError(out IStackError innerStackError)
            ? innerStackError.StackTrace
            : StackTrace;

    public ExceptionError()
    {
    }

    public ExceptionError(Exception exception, string message = null)
        : base(message ?? exception.Message)
    {
        Exception = exception;
        StackTrace = Exception.StackTrace;
    }
}