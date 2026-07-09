using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces.Flow;
using System;

namespace FEx.Agnostics.Abstractions.Flow;

public class ExceptionError : Error, IExceptionError
{
    public Exception? Exception { get; }

    public string? StackTrace { get; }

    public string? RootErrorStackTrace =>
        InnerError is not null && InnerError.TryGetError<IStackError>(out var innerStackError)
            ? innerStackError.StackTrace
            : StackTrace;

    public ExceptionError()
    {
    }

    public ExceptionError(Exception exception, string? message = null)
        : base(message ?? exception.Message)
    {
        Exception = exception;
        StackTrace = Exception.StackTrace;
    }
}