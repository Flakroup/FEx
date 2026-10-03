using System;

namespace FEx.Agnostics.Abstractions.Flow;

/// <summary>Event data describing a logged error.</summary>
public class FExErrorEventArgs : EventArgs
{
    /// <summary>Gets the error message.</summary>
    public string? Message { get; }
    /// <summary>Gets the exception associated with the error, if any.</summary>
    public Exception? Exception { get; }

    /// <summary>Initializes the event data with a message.</summary>
    /// <param name="message">The error message.</param>
    public FExErrorEventArgs(string? message)
        : this(message, null)
    {
    }

    /// <summary>Initializes the event data with a message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="exception">The exception associated with the error.</param>
    public FExErrorEventArgs(string? message, Exception? exception)
    {
        Message = message;
        Exception = exception;
    }
}