using FEx.Agnostics.Abstractions.Utilities;

namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>An object whose change notifications can be temporarily suppressed.</summary>
public interface ISuppressEvents
{
    /// <summary>Gets a value indicating whether events are currently suppressed.</summary>
    bool EventsAreSuppressed { get; }
    /// <summary>Gets or sets the number of active suppression scopes; events are suppressed while it is greater than zero.</summary>
    int SuppressedEvents { get; set; }

    /// <summary>Starts suppressing events until the returned object is disposed.</summary>
    /// <returns>A disposable that ends the suppression.</returns>
    SuppressEventsDisposable SuppressEvents();
}