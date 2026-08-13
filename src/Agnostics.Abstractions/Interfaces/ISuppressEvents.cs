using FEx.Agnostics.Abstractions.Utilities;

namespace FEx.Agnostics.Abstractions.Interfaces;

public interface ISuppressEvents
{
    bool EventsAreSuppressed { get; }
    int SuppressedEvents { get; set; }

    SuppressEventsDisposable SuppressEvents();
}