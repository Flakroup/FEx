using FEx.Agnostics.Abstractions.Configuration;
using FEx.Agnostics.Abstractions.Helpers;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FEx.Agnostics.Abstractions.Collections.Concurrent;

/// <summary>Base class for thread-safe lists that raise property and collection change notifications through overridable hooks.</summary>
/// <typeparam name="T">The element type.</typeparam>
public abstract class BaseConcurrentList<T> : ISuppressEvents
{
    /// <summary>Gets the configuration of the collection events.</summary>
    public CollectionEventsConfig Config { get; }

    /// <summary>Gets a value indicating whether events are currently suppressed.</summary>
    public bool EventsAreSuppressed => ((ISuppressEvents)this).SuppressedEvents > 0;
    int ISuppressEvents.SuppressedEvents { get; set; }

    /// <summary>Initializes the list with a default <see cref="CollectionEventsConfig" />.</summary>
    protected BaseConcurrentList()
    {
        Config = new();
    }

    /// <inheritdoc />
    public SuppressEventsDisposable SuppressEvents() => new(this);

    /// <summary>Raises the change notification for the <c>Count</c> property.</summary>
    protected virtual void OnCountPropertyChanged() => OnPropertyChanged(EventArgsCache.CountPropertyChanged);

    /// <summary>Raises the change notification for the indexer (<c>Item[]</c>) property.</summary>
    protected virtual void OnIndexerPropertyChanged() => OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);

    /// <summary>Raises a property change notification; the base implementation does nothing.</summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
    }

    /// <summary>Raises an add notification for a single item.</summary>
    /// <param name="item">The added item.</param>
    /// <param name="index">The index the item was added at.</param>
    protected virtual void OnAddToCollection(T item, int index) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Add, item, index));

    /// <summary>Raises an add notification for a range of items.</summary>
    /// <param name="items">The added items.</param>
    /// <param name="index">The index the first item was added at.</param>
    protected virtual void OnAddRangeToCollection(IList<T> items, int index) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Add, items, index));

    /// <summary>Raises a remove notification for a single item.</summary>
    /// <param name="item">The removed item.</param>
    /// <param name="index">The index the item was removed from; reported as -1 unless <see cref="CollectionEventsConfig.PassIndexOfRemovedItem" /> is set.</param>
    protected virtual void OnRemoveFromCollection(T item, int index) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Remove,
            item,
            Config.PassIndexOfRemovedItem
                ? index
                : -1));

    /// <summary>Raises a remove notification for a range of items.</summary>
    /// <param name="items">The removed items.</param>
    /// <param name="index">The index the first item was removed from; reported as -1 unless <see cref="CollectionEventsConfig.PassIndexOfRemovedItem" /> is set.</param>
    protected virtual void OnRemoveFromCollection(IList<T> items, int index) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Remove,
            items,
            Config.PassIndexOfRemovedItem
                ? index
                : -1));

    /// <summary>Raises a move notification.</summary>
    /// <param name="item">The moved item.</param>
    /// <param name="index">The new index.</param>
    /// <param name="oldIndex">The previous index.</param>
    protected virtual void OnMoveInCollection(T item, int index, int oldIndex) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Move, item, index, oldIndex));

    /// <summary>Raises a replace notification for a single item.</summary>
    /// <param name="item">The new item.</param>
    /// <param name="oldItem">The replaced item.</param>
    /// <param name="index">The index of the replaced item.</param>
    protected virtual void OnReplaceInCollection(T item, T oldItem, int index) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Replace, item, oldItem, index));

    /// <summary>Raises a reset notification.</summary>
    protected virtual void OnCollectionReset() => OnCollectionChanged(EventArgsCache.ResetCollectionChanged);

    /// <summary>Raises a replace notification for lists of items.</summary>
    /// <param name="newItems">The new items.</param>
    /// <param name="oldItems">The replaced items.</param>
    protected virtual void OnCollectionChanged(IList newItems, IList oldItems) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Replace, newItems, oldItems));

    /// <summary>Raises a collection change notification; the base implementation does nothing.</summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
    }
}