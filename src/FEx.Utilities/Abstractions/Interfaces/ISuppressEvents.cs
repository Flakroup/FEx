using FEx.Utilities.Basics;

namespace FEx.Utilities.Abstractions.Interfaces;

public interface ISuppressEvents
{
    int SuppressedEvents { get; set; }
    SuppressEventsDisposable SuppressEvents();
}