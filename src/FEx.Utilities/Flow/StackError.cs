using FEx.Extensions;
using System;

namespace FEx.Utilities.Flow;

public class StackError : Error
{
    public StackError()
    {
    }

    public StackError(string message = null)
    {
        Message = message;
    }

    public StackError(Error innerError, string message = null)
        : base(innerError)
    {
        Message = message;
    }

    public static implicit operator StackError(string message)
    {
        return new()
        {
            Message = message
        };
    }

    public override string ToString()
    {
        if (Message.IsNotNullOrEmptyString())
            return Message + Environment.NewLine + StackTrace;

        return StackTrace;
    }
}

public class StackError<TErrorStatus> : Error<TErrorStatus>
{
    public StackError(TErrorStatus status)
        : base(status)
    {
    }

    public StackError(TErrorStatus status, string message = null)
        : base(status)
    {
        Message = message;
    }

    public StackError(TErrorStatus status, Error innerError, string message = null)
        : base(status, innerError)
    {
        Message = message;
    }

    public override string ToString()
    {
        if (Message.IsNotNullOrEmptyString())
            return Message + Environment.NewLine + StackTrace;

        return StackTrace;
    }
}