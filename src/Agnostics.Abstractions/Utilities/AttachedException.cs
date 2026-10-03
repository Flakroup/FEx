using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Diagnostics;

namespace FEx.Agnostics.Abstractions.Utilities;

/// <summary>An exception that carries a stack trace captured elsewhere, for example on another thread.</summary>
public class AttachedException : Exception
{
    /// <summary>Wraps an exception thrown by a sender.</summary>
    /// <param name="sender">The object that threw; its type name is put in the message.</param>
    /// <param name="stackTrace">The stack trace to attach.</param>
    /// <param name="innerException">The original exception.</param>
    public AttachedException(object sender, StackTrace stackTrace, Exception innerException)
        : base($"{sender.GetType().FullName} thrown: {innerException.Message}", innerException)
    {
        this.SetStackTrace(stackTrace);
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="stackTrace">The stack trace to attach.</param>
    public AttachedException(string message, StackTrace stackTrace)
        : base(message)
    {
        this.SetStackTrace(stackTrace);
    }
}