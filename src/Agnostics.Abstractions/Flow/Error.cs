using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces.Flow;
using System;

namespace FEx.Agnostics.Abstractions.Flow;

public class Error : IError
{
    private IError _innerError;

    public string Message { get; }
    public IError RootError { get; private set; }

    public IError InnerError
    {
        get => _innerError;
        private set
        {
            _innerError = value.GuardProperty();
            RootError = InnerError.RootError ?? InnerError;
        }
    }

    public Error()
    {
    }

    public Error(string message)
    {
        Message = message;
    }

    public Error(IError innerError)
        : this(innerError, null)
    {
    }

    public Error(IError innerError, string message)
        : this(message)
    {
        InnerError = innerError;
    }

    public void SetInnerError(IError innerError)
    {
        if (InnerError is not null)
            throw new InvalidOperationException($"{nameof(InnerError)} is already set");

        InnerError = innerError;
    }

    public static implicit operator Error(string message) => new(message);
}

public class Error<TErrorStatus> : Error
{
    public TErrorStatus Status { get; }

    public Error(TErrorStatus status)
        : this(status, (string)null)
    {
    }

    public Error(TErrorStatus status, string message)
        : base(message)
    {
        Status = status;
    }

    public Error(TErrorStatus status, IError innerError)
        : this(status, innerError, null)
    {
    }

    public Error(TErrorStatus status, IError innerError, string message)
        : base(innerError, message)
    {
        Status = status;
    }
}