using FEx.Agnostics.Abstractions.Interfaces;
using System;

namespace FEx.Agnostics.Abstractions.Utilities;

/// <summary>Suppresses the events of a source while it is alive and restores them when disposed.</summary>
public sealed class SuppressEventsDisposable : DisposableAction
{
    /// <summary>Starts suppressing the events of a source.</summary>
    /// <param name="suppressedEventSource">The source whose suppression counter is incremented now and decremented on disposal.</param>
    public SuppressEventsDisposable(ISuppressEvents suppressedEventSource)
        : this(suppressedEventSource, null)
    {
    }

    /// <summary>Starts suppressing the events of a source and runs a callback once the last suppression ends.</summary>
    /// <param name="suppressedEventSource">The source whose suppression counter is incremented now and decremented on disposal.</param>
    /// <param name="onNoMoreSuppressedEvents">Invoked on disposal when the counter has dropped to zero.</param>
    public SuppressEventsDisposable(ISuppressEvents suppressedEventSource, Action? onNoMoreSuppressedEvents)
        : base(() => Act(suppressedEventSource, onNoMoreSuppressedEvents))
    {
        ++suppressedEventSource.SuppressedEvents;
    }

    private static void Act(ISuppressEvents suppressedEventSource, Action? onNoMoreSuppressedEvents)
    {
        var suppressedEventsCount = --suppressedEventSource.SuppressedEvents;

        if (suppressedEventsCount == 0)
            onNoMoreSuppressedEvents?.Invoke();
    }
}