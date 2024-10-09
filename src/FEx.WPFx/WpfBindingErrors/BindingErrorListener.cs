using System;
using System.Diagnostics;

namespace FEx.WPFx.WpfBindingErrors;

/// <summary>
///     Raises an event each time a WPF Binding error occurs.
/// </summary>
/// <remarks>
///     WPF Binding Error Testing
///     Copyright 2013 Benoit Blanchon
///     This has been inpired by
///     http://tech.pro/tutorial/940/wpf-snippet-detecting-binding-errors
/// </remarks>
public sealed class BindingErrorListener : IDisposable
{
    private readonly ObservableTraceListener _traceListener;

    /// <summary>
    ///     Event raised each time a WPF binding error occurs
    /// </summary>
    public event Action<TraceEventCache, string, TraceEventType, string> ErrorCatched
    {
        add => _traceListener.TraceCatched += value;
        remove => _traceListener.TraceCatched -= value;
    }

    public BindingErrorListener()
    {
        _traceListener = new();
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        PresentationTraceSources.DataBindingSource.Listeners.Add(_traceListener);
    }

    static BindingErrorListener()
    {
        PresentationTraceSources.Refresh();
    }

    #region IDisposable
    public void Dispose()
    {
        PresentationTraceSources.DataBindingSource.Listeners.Remove(_traceListener);
        _traceListener.Dispose();
    }
    #endregion
}