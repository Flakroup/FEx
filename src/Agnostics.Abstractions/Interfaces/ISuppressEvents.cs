using FEx.Basics.Utilities;

namespace FEx.Basics.Abstractions.Interfaces;

public interface ISuppressEvents
{
    bool EventsAreSuppressed { get; }
    int SuppressedEvents { get; set; }

    SuppressEventsDisposable SuppressEvents();
}