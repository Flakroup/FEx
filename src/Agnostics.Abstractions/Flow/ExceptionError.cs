using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces.Flow;
using System;

namespace FEx.Agnostics.Abstractions.Flow;

/// <summary>An error that wraps an exception and captures its stack trace.</summary>
public class ExceptionError : Error, IExceptionError
{
    /// <inheritdoc />
    public Exception? Exception { get; }

    /// <inheritdoc />
    public string? StackTrace { get; }

    /// <inheritdoc />
    public string? RootErrorStackTrace =>
        InnerError is not null && InnerError.TryGetError<IStackError>(out var innerStackError)
            ? innerStackError.StackTrace
            : StackTrace;

    /// <summary>Initializes an error without an exception.</summary>
    public ExceptionError()
    {
    }

    /// <summary>Initializes an error from an exception</summary>
    /// <param name="exception">The exception to wrap.</param>
    /// <param name="message">The error message; the exception message when null.</param>
    public ExceptionError(Exception exception, string? message = null)
        : base(message ?? exception.Message)
    {
        Exception = exception;
        StackTrace = Exception.StackTrace;
    }
}