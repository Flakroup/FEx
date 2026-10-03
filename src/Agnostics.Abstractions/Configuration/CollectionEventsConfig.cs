namespace FEx.Agnostics.Abstractions.Configuration;

/// <summary>Configures how collection change events are raised.</summary>
public class CollectionEventsConfig
{
    /// <summary>Gets or sets a value indicating whether removal notifications carry the index of the removed item; when false the index is reported as -1.</summary>
    public bool PassIndexOfRemovedItem { get; set; }
}