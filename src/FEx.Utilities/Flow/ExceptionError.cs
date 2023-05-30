using System;

namespace FEx.Utilities.Flow;

public class ExceptionError : StackError
{
    public Exception Exception { get; }

    public ExceptionError()
    {
    }

    public ExceptionError(Exception ex)
        : base(ex.Message)
    {
        Exception = ex;
    }
}