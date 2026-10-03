using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces.Flow;
using System;

namespace FEx.Agnostics.Abstractions.Flow;

/// <summary>Base implementation of <see cref="IError" /> with a message and an optional chain of inner errors.</summary>
public class Error : IError
{
    private IError? _innerError;

    /// <inheritdoc />
    public string? Message { get; }
    /// <inheritdoc />
    public IError? RootError { get; private set; }

    /// <inheritdoc />
    public IError? InnerError
    {
        get => _innerError;
        private set
        {
            _innerError = value.GuardProperty();
            RootError = _innerError.RootError ?? _innerError;
        }
    }

    /// <summary>Initializes an error without a message.</summary>
    public Error()
    {
    }

    /// <summary>Initializes an error with a message.</summary>
    /// <param name="message">The error message.</param>
    public Error(string? message)
    {
        Message = message;
    }

    /// <summary>Initializes an error caused by another error.</summary>
    /// <param name="innerError">The inner error.</param>
    public Error(IError innerError)
        : this(innerError, null)
    {
    }

    /// <summary>Initializes an error with a message that was caused by another error.</summary>
    /// <param name="innerError">The inner error.</param>
    /// <param name="message">The error message.</param>
    public Error(IError innerError, string? message)
        : this(message)
    {
        InnerError = innerError;
    }

    /// <inheritdoc />
    public void SetInnerError(IError innerError)
    {
        if (InnerError is not null)
            throw new InvalidOperationException($"{nameof(InnerError)} is already set");

        InnerError = innerError;
    }

    /// <summary>Creates an error from a message.</summary>
    /// <param name="message">The error message.</param>
    public static implicit operator Error(string message) => new(message);
}

/// <summary>An error that additionally carries a status value.</summary>
/// <typeparam name="TErrorStatus">The status type.</typeparam>
public class Error<TErrorStatus> : Error
{
    /// <summary>Gets the status describing the kind of error.</summary>
    public TErrorStatus Status { get; }

    /// <summary>Initializes an error with a status.</summary>
    /// <param name="status">The error status.</param>
    public Error(TErrorStatus status)
        : this(status, (string?)null)
    {
    }

    /// <summary>Initializes an error with a status and a message.</summary>
    /// <param name="status">The error status.</param>
    /// <param name="message">The error message.</param>
    public Error(TErrorStatus status, string? message)
        : base(message)
    {
        Status = status;
    }

    /// <summary>Initializes an error with a status that was caused by another error.</summary>
    /// <param name="status">The error status.</param>
    /// <param name="innerError">The inner error.</param>
    public Error(TErrorStatus status, IError innerError)
        : this(status, innerError, null)
    {
    }

    /// <summary>Initializes an error with a status and a message that was caused by another error.</summary>
    /// <param name="status">The error status.</param>
    /// <param name="innerError">The inner error.</param>
    /// <param name="message">The error message.</param>
    public Error(TErrorStatus status, IError innerError, string? message)
        : base(innerError, message)
    {
        Status = status;
    }
}