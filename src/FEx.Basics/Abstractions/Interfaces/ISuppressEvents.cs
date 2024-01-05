using FEx.Basics.Utilities;

namespace FEx.Basics.Abstractions.Interfaces;

public interface ISuppressEvents
{
    int SuppressedEvents { get; set; }
    SuppressEventsDisposable SuppressEvents();
}