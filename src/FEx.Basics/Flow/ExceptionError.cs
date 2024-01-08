using FEx.Basics.Extensions;
using System;

namespace FEx.Basics.Flow;

public class ExceptionError : Error, IExceptionError
{
    public Exception Exception { get; }

    public string StackTraceString { get; }

    public string RootErrorStackTraceString => InnerError.TryGetError(out IStackError innerStackError)
        ? innerStackError.StackTraceString
        : StackTraceString;

    public ExceptionError()
    {
    }

    public ExceptionError(Exception exception, string message = null)
        : base(message ?? exception.Message)
    {
        Exception = exception;
        StackTraceString = Exception.StackTrace;
    }
}