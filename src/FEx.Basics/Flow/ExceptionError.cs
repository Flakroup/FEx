using System;

namespace FEx.Basics.Flow;

public class ExceptionError : Error, IStackError
{
    public Exception Exception { get; }

    public string StackTrace { get; }
    public string RootErrorStackTrace => (RootError as IStackError)?.StackTrace;

    public ExceptionError()
    {
    }

    public ExceptionError(Exception exception)
        : base(exception.Message)
    {
        Exception = exception;
        StackTrace = Exception.StackTrace;
    }
}