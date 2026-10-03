using System;

namespace FEx.Agnostics.Exceptions;

/// <summary>The base exception type for errors raised by FEx libraries.</summary>
[Serializable]
public class FExException : Exception
{
    /// <summary>Initializes the exception without a message.</summary>
    public FExException()
    {
    }

    /// <summary>Initializes the exception with a message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public FExException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes the exception with a message and the exception that caused it.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public FExException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}