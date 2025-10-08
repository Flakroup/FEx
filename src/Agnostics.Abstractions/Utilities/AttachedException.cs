using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Diagnostics;

namespace FEx.Agnostics.Abstractions.Utilities;

public class AttachedException : Exception
{
    public AttachedException(object sender, StackTrace stackTrace, Exception innerException)
        : base($"{sender.GetType().FullName} thrown: {innerException.Message}", innerException)
    {
        this.SetStackTrace(stackTrace);
    }

    public AttachedException(string message, StackTrace stackTrace)
        : base(message)
    {
        this.SetStackTrace(stackTrace);
    }
}