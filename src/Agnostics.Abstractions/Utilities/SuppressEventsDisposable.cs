using FEx.Agnostics.Abstractions.Interfaces;
using System;

namespace FEx.Agnostics.Abstractions.Utilities;

public sealed class SuppressEventsDisposable : DisposableAction
{
    public SuppressEventsDisposable(ISuppressEvents suppressedEventSource)
        : this(suppressedEventSource, null)
    {
    }

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