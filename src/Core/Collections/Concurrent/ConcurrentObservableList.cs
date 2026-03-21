using DynamicData.Binding;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Agnostics.Collections.Concurrent;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;

namespace FEx.Core.Collections.Concurrent;

[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
[Serializable]
public class ConcurrentObservableList<T> : ConcurrentList<T>, IObservableCollection<T>
{
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
        Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(ev =>
                CollectionChanged += ev,
            ev => CollectionChanged -= ev);

    /// <summary>
    /// Initializes a new instance of the ConcurrentObservableList class that contains
    /// elements copied from the specified collection and has sufficient capacity
    /// to accommodate the number of elements copied.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new list.</param>
    public ConcurrentObservableList()
        : this(null)
    {
    }

    public ConcurrentObservableList(IEnumerable<T> collection)
        : base(collection)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        _dispatcher = FExCoreStatics.Dispatcher;
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

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (EventsAreSuppressed || PropertyChanged is null)
            return;

        _dispatcher.SendInContext(() => PropertyChanged.HandlePropertyChanged(this, e), this);
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (EventsAreSuppressed || CollectionChanged is null)
            return;

        _dispatcher.SendInContext(() => CollectionChanged.Invoke(this, e), this);
    }
}