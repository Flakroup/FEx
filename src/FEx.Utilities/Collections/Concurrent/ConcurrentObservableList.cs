using FEx.Abstractions;
using FEx.Utilities.Basics;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace FEx.Utilities.Collections.Concurrent;

[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[Serializable]
public class ConcurrentObservableList<T> : ConcurrentList<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly SynchronizationContext _synchronizationContext = SynchronizationContext.Current ?? Dispatcher.MainThreadSynchronizationContext;

    private static IFExDispatcher Dispatcher => Fundamentals.Dispatcher;

    public ConcurrentObservableList(IEnumerable<T> collection = null)
        : base(collection)
    {
    }

    /// <summary>
    ///     Occurs when the collection changes, either by adding or removing an item.
    /// </summary>
    [field: NonSerialized]
    public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

    /// <summary>
    ///     PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
    /// </summary>
    event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
    {
        add => PropertyChanged += value;
        remove => PropertyChanged -= value;
    }

    /// <summary>
    ///     Move item at oldIndex to newIndex.
    /// </summary>
    public void Move(int oldIndex, int newIndex)
    {
        MoveItem(oldIndex, newIndex);
    }

    /// <summary>
    ///     PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
    /// </summary>
    [field: NonSerialized]
    protected virtual event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    ///     Called by base class ObservableCollection&lt;T&gt; when an item is to be moved within the list;
    ///     raises a CollectionChanged event to any listeners.
    /// </summary>
    protected virtual void MoveItem(int oldIndex, int newIndex)
    {
        Write(() =>
        {
            T removedItem = this[oldIndex];

            using (SuppressEvents())
            {
                Remove(oldIndex);
                Insert(newIndex, removedItem);
            }

            OnIndexerPropertyChanged();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, removedItem, newIndex, oldIndex));
        });
    }

    /// <summary>
    ///     Raises a PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
    /// </summary>
    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!EventsAreSuppressed)
            Dispatch(() => PropertyChanged?.Invoke(this, e));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!EventsAreSuppressed)
            Dispatch(() => CollectionChanged?.Invoke(this, e));
    }

    /// <summary>
    ///     Helper to raise a PropertyChanged event for the Count property
    /// </summary>
    protected override void OnCountPropertyChanged()
    {
        OnPropertyChanged(EventArgsCache.CountPropertyChanged);
    }

    /// <summary>
    ///     Helper to raise a PropertyChanged event for the Indexer property
    /// </summary>
    protected override void OnIndexerPropertyChanged()
    {
        OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);
    }

    /// <summary>
    ///     Helper to raise CollectionChanged event with action == Reset to any listeners
    /// </summary>
    protected override void OnCollectionReset()
    {
        OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
    }

    private void Dispatch(Action action)
    {
        _synchronizationContext.Send(_ => action(), default);
    }
}