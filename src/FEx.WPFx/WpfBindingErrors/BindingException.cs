using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace FEx.WPFx.WpfBindingErrors;

/// <summary>
/// Exception thrown by the BindingExceptionThrower each time a WPF binding error occurs
/// </summary>
/// <remarks>
/// WPF Binding Error Testing
/// Copyright 2013 Benoit Blanchon
/// This has been inpired by
/// http://tech.pro/tutorial/940/wpf-snippet-detecting-binding-errors
/// </remarks>
[Serializable]
[JsonObject]
public class BindingException : Exception
{
    public override string StackTrace { get; }
    public DateTime OccurenceTime { get; }

    public BindingException(TraceEventCache eventCache, string source, string message)
        : base(message)
    {
        OccurenceTime = eventCache.DateTime;
        StackTrace = eventCache.Callstack;
        Source = source;
    }

    public BindingException(string message)
        : base(message)
    {
    }

    public BindingException()
    {
    }

    public BindingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public override bool Equals(object obj) => Equals(obj as BindingException);

    public override int GetHashCode() => Message.GetHashCode();

    protected bool Equals(BindingException other) => string.Equals(Message, other?.Message);
}