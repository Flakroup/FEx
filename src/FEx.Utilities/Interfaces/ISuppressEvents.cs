using FEx.Utilities.Basics;

namespace FEx.Utilities.Interfaces;

public interface ISuppressEvents
{
    int SuppressedEvents { get; set; }
    SuppressEventsDisposable SuppressEvents();
}