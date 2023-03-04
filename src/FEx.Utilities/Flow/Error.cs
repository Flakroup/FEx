using FEx.Extensions;
using System;

namespace FEx.Utilities.Flow;

public interface IError
{
    Error InnerError { get; }
    string Message { get; set; }
    string RootErrorStackTrace { get; }
    string StackTrace { get; }

    void SetInnerError(Error innerError);
}

public abstract class Error : IError
{
    private Error _innerError;

    public string Message { get; set; }
    public string StackTrace { get; }
    public IError RootError { get; private set; }

    public Error InnerError
    {
        get => _innerError;
        set
        {
            _innerError = value.Guard(nameof(InnerError));
            RootError = InnerError.RootError ?? InnerError;
        }
    }

    public string RootErrorStackTrace => RootError?.StackTrace;

    protected Error()
    {
        StackTrace = Fundamentals.StackTraceGenerator.GetCachedStackTrace()
            .ToString();
    }

    protected Error(Error innerError)
        : this()
    {
        InnerError = innerError;
    }

    public void SetInnerError(Error innerError)
    {
        if (InnerError != null)
            throw new InvalidOperationException($"{nameof(InnerError)} is already set");

        InnerError = innerError;
    }
}

public abstract class Error<TErrorStatus> : Error
{
    public TErrorStatus Status { get; }

    public Error(TErrorStatus status)
    {
        Status = status;
    }

    public Error(TErrorStatus status, Error innerError)
        : base(innerError)
    {
        Status = status;
    }
}