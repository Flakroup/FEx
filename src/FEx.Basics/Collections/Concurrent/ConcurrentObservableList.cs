using DynamicData.Binding;
using FEx.Abstractions;
using FEx.Abstractions.Interfaces;
using FEx.Basics.Utilities;
using FEx.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;

namespace FEx.Basics.Collections.Concurrent;

[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[Serializable]
public class ConcurrentObservableList<T> : ConcurrentList<T>, IObservableCollection<T>
{
    [NonSerialized]
    private readonly IFExDispatcher _dispatcher;

    /// <summary>
    /// Occurs when the collection changes, either by adding or removing an item.
    /// </summary>
    [field: NonSerialized]
    public event NotifyCollectionChangedEventHandler CollectionChanged;

    /// <summary>
    /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
    /// </summary>
    [field: NonSerialized]
    public event PropertyChangedEventHandler PropertyChanged;

    public IObservable<EventPattern<NotifyCollectionChangedEventArgs>> CollectionChangedObservable =>
        Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
            ev => CollectionChanged += ev,
            ev => CollectionChanged -= ev);

    /// <summary>
    /// Initializes a new instance of the ConcurrentObservableList class that contains
    /// elements copied from the specified collection and has sufficient capacity
    /// to accommodate the number of elements copied.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new list.</param>
    public ConcurrentObservableList(IEnumerable<T> collection = null)
        : base(collection)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        _dispatcher = FExFoundation.Dispatcher;
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Suspends count notifications.
    /// </summary>
    /// <returns>A disposable when disposed will reset the count.</returns>
    public IDisposable SuspendCount() => new SuppressEventsDisposable(this, ResumeEvents);

    /// <summary>
    /// Suspends notifications. When disposed, a reset notification is fired.
    /// </summary>
    /// <returns>A disposable when disposed will reset notifications.</returns>
    public IDisposable SuspendNotifications() => new SuppressEventsDisposable(this, ResumeEvents);

    /// <summary>
    /// Clears the list and Loads the specified items.
    /// </summary>
    /// <param name="items">The items.</param>
    public void Load(IEnumerable<T> items) => ReplaceWith(items);

    protected virtual void ResumeEvents()
    {
        if (!EventsAreSuppressed)
            OnCollectionReset();
    }

    protected virtual void Dispatch(Action action) => _dispatcher.InvokeOnMainThread(action, this);

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (EventsAreSuppressed || PropertyChanged is null)
            return;

        Dispatch(() => PropertyChanged.HandlePropertyChanged(this, e));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (EventsAreSuppressed || CollectionChanged is null)
            return;

        Dispatch(() => CollectionChanged.Invoke(this, e));
    }
}