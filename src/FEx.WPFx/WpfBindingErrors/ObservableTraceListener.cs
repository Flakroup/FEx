using System;
using System.Diagnostics;
using System.Text;

namespace FEx.WPFx.WpfBindingErrors;

/// <summary>
///     A TraceListener that raise an event each time a trace is written
/// </summary>
/// <remarks>
///     WPF Binding Error Testing
///     Copyright 2013 Benoit Blanchon
///     This has been inpired by
///     http://tech.pro/tutorial/940/wpf-snippet-detecting-binding-errors
/// </remarks>
internal sealed class ObservableTraceListener : TraceListener
{
    public event Action<TraceEventCache, string, TraceEventType, string> TraceCatched;
    private StringBuilder Buffer { get; } = new();

    [DebuggerStepThrough]
    public override void Write(string message) => Buffer.Append(message);

    [DebuggerStepThrough]
    public override void WriteLine(string message)
    {
        Buffer.Append(message);

        //TraceCatched?.Invoke(buffer.ToString());

        Buffer.Clear();
    }

    [DebuggerStepThrough]
    public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
    {
        base.TraceEvent(eventCache, source, eventType, id);
        TraceCatched?.Invoke(eventCache, source, eventType, null);
    }

    [DebuggerStepThrough]
    public override void TraceEvent(TraceEventCache eventCache,
                                    string source,
                                    TraceEventType eventType,
                                    int id,
                                    string message)
    {
        base.TraceEvent(eventCache, source, eventType, id, message);
        TraceCatched?.Invoke(eventCache, source, eventType, message);
    }

    [DebuggerStepThrough]
    public override void TraceEvent(TraceEventCache eventCache,
                                    string source,
                                    TraceEventType eventType,
                                    int id,
                                    string format,
                                    params object[] args)
    {
        base.TraceEvent(eventCache, source, eventType, id, format, args);
        TraceCatched?.Invoke(eventCache, source, eventType, format);
    }
}