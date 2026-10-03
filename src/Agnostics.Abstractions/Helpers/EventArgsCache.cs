using System.Collections.Specialized;
using System.ComponentModel;

namespace FEx.Agnostics.Abstractions.Helpers;

/// <summary>Cached event argument instances that avoid allocating a new object for every notification.</summary>
public static class EventArgsCache
{
    /// <summary>Event arguments for a change of the <c>Count</c> property.</summary>
    public static readonly PropertyChangedEventArgs CountPropertyChanged = new("Count");

    /// <summary>Event arguments for a change of the <c>IsEmpty</c> property.</summary>
    public static readonly PropertyChangedEventArgs IsEmptyPropertyChanged = new("IsEmpty");

    /// <summary>Event arguments for a change of the indexer (<c>Item[]</c>).</summary>
    public static readonly PropertyChangedEventArgs IndexerPropertyChanged = new("Item[]");

    /// <summary>Event arguments for a collection reset.</summary>
    public static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged =
        new(NotifyCollectionChangedAction.Reset);
}