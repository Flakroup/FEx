using FEx.Basics.Abstractions.Interfaces;
using FEx.Basics.Collections.Concurrent;
using FEx.Basics.Utilities;
using FEx.Basics.Utilities.Collections;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FEx.Basics.Abstractions;

public abstract class BaseConcurrentList<T> : ISuppressEvents
{
    public CollectionEventsConfig Config { get; }

    public bool EventsAreSuppressed => ((ISuppressEvents)this).SuppressedEvents > 0;
    int ISuppressEvents.SuppressedEvents { get; set; }

    protected BaseConcurrentList()
    {
        Config = new();
    }

    /// <inheritdoc />
    public SuppressEventsDisposable SuppressEvents() => new(this);

    protected virtual void OnCountPropertyChanged() => OnPropertyChanged(EventArgsCache.CountPropertyChanged);

    protected virtual void OnIndexerPropertyChanged() => OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
    }

    protected virtual void OnAddToCollection(T item, int index) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Add, item, index));

    protected virtual void OnAddRangeToCollection(IList<T> items, int index) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Add, items, index));

    protected virtual void OnRemoveFromCollection(T item, int index) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Remove,
            item,
            Config.PassIndexOfRemovedItem
                ? index
                : -1));

    protected virtual void OnRemoveFromCollection(IList<T> items, int index) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Remove,
            items,
            Config.PassIndexOfRemovedItem
                ? index
                : -1));

    protected virtual void OnMoveInCollection(T item, int index, int oldIndex) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Move, item, index, oldIndex));

    protected virtual void OnReplaceInCollection(T item, T oldItem, int index) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Replace, item, oldItem, index));

    protected virtual void OnCollectionReset() => OnCollectionChanged(EventArgsCache.ResetCollectionChanged);

    protected virtual void OnCollectionChanged(IList newItems, IList oldItems) =>
        OnCollectionChanged(new(NotifyCollectionChangedAction.Replace, newItems, oldItems));

    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
    }
}