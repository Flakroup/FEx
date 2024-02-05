using FEx.Abstractions.Interfaces;
using FEx.Basics.Utilities.Collections;
using FEx.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;

namespace FEx.Basics.Collections.Concurrent;

[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[Serializable]
public class ConcurrentObservableList<T> : ConcurrentList<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly SynchronizationContext _synchronizationContext = SynchronizationContext.Current;
    private readonly bool _sendEventsInCreationContext;

    /// <summary>
    ///     Occurs when the collection changes, either by adding or removing an item.
    /// </summary>
    [field: NonSerialized]
    public event NotifyCollectionChangedEventHandler CollectionChanged;

    /// <summary>
    ///     PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
    /// </summary>
    [field: NonSerialized]
    protected event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    ///     PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
    /// </summary>
    event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
    {
        add => PropertyChanged += value;
        remove => PropertyChanged -= value;
    }

    public IObservable<EventPattern<NotifyCollectionChangedEventArgs>> CollectionChangedObservable =>
        Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
            ev => CollectionChanged += ev, ev => CollectionChanged -= ev);

    private static IEventDeliverer EventDeliverer => FExBasics.EventDeliverer;

    /// <summary>
    ///     Initializes a new instance of the ConcurrentObservableList class that contains
    ///     elements copied from the specified collection and has sufficient capacity
    ///     to accommodate the number of elements copied.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new list.</param>
    /// <param name="sendEventsInCreationContext">
    ///     Overrides setting from Foundation.SendEventsInCreationContext.
    ///     If true sends all events using SynchronizationContext of thread in which was this constructor executed.
    /// </param>
    public ConcurrentObservableList(IEnumerable<T> collection = null, bool? sendEventsInCreationContext = null)
        : base(collection)
    {
        _sendEventsInCreationContext = sendEventsInCreationContext ?? FExBasics.SendEventsInCreationContext;
    }

    /// <summary>
    ///     Move item at oldIndex to newIndex.
    /// </summary>
    public void Move(int oldIndex, int newIndex) => MoveItem(oldIndex, newIndex);

    /// <summary>
    ///     Called by base class ObservableCollection&lt;T&gt; when an item is to be moved within the list;
    ///     raises a CollectionChanged event to any listeners.
    /// </summary>
    protected virtual void MoveItem(int oldIndex, int newIndex) => Write(() =>
    {
        T removedItem = this[oldIndex];

        using (SuppressEvents())
        {
            Remove(oldIndex);
            Insert(newIndex, removedItem);
        }

        OnIndexerPropertyChanged();

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, removedItem,
            newIndex, oldIndex));
    });

    /// <summary>
    ///     Raises a PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
    /// </summary>
    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!EventsAreSuppressed
            && PropertyChanged is not null)
            Dispatch(() => PropertyChanged.HandlePropertyChanged(this, e));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!EventsAreSuppressed
            && CollectionChanged is not null)
            Dispatch(() => CollectionChanged.Invoke(this, e));
    }

    /// <summary>
    ///     Helper to raise a PropertyChanged event for the Count property
    /// </summary>
    protected override void OnCountPropertyChanged() => OnPropertyChanged(EventArgsCache.CountPropertyChanged);

    /// <summary>
    ///     Helper to raise a PropertyChanged event for the Indexer property
    /// </summary>
    protected override void OnIndexerPropertyChanged() => OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);

    /// <summary>
    ///     Helper to raise CollectionChanged event with action == Reset to any listeners
    /// </summary>
    protected override void OnCollectionReset() => OnCollectionChanged(EventArgsCache.ResetCollectionChanged);

    private void Dispatch(Action action) => EventDeliverer.DeliverEvent(action, this, _sendEventsInCreationContext
        ? _synchronizationContext
        : null);
}