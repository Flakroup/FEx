using FEx.Utilities.Interfaces;
using System;

namespace FEx.Utilities.Basics
{
    public readonly struct SuppressEventsDisposable : IDisposable
    {
        private readonly ISuppressEvents _suppressedEventSource;

        public SuppressEventsDisposable(ISuppressEvents suppressedEventSource)
        {
            _suppressedEventSource = suppressedEventSource;
            ++suppressedEventSource.SuppressedEvents;
        }

        public void Dispose()
        {
            --_suppressedEventSource.SuppressedEvents;
        }
    }
}