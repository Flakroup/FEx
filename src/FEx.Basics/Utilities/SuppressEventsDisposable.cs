using FEx.Basics.Abstractions.Interfaces;
using System;

namespace FEx.Basics.Utilities;

public readonly struct SuppressEventsDisposable : IDisposable
{
    private readonly ISuppressEvents _suppressedEventSource;

    public SuppressEventsDisposable(ISuppressEvents suppressedEventSource)
    {
        _suppressedEventSource = suppressedEventSource;
        ++suppressedEventSource.SuppressedEvents;
    }

    public void Dispose() => --_suppressedEventSource.SuppressedEvents;
}