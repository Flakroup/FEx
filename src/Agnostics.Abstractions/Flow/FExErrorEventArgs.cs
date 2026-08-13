using System;

namespace FEx.Agnostics.Abstractions.Flow;

public class FExErrorEventArgs : EventArgs
{
    public string? Message { get; }
    public Exception? Exception { get; }

    public FExErrorEventArgs(string? message)
        : this(message, null)
    {
    }

    public FExErrorEventArgs(string? message, Exception? exception)
    {
        Message = message;
        Exception = exception;
    }
}