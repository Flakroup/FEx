using FEx.Extensions;
using System;
using System.Diagnostics;

namespace FEx.Utilities.Exceptions;

public class AttachedException : Exception
{
    public AttachedException(string message, StackTrace stackTrace)
        : base(message)
    {
        this.SetStackTrace(stackTrace);
    }
}