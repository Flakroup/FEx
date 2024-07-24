using DynamicData.Binding;
using FEx.Basics.Abstractions.Interfaces.Collections;
using FEx.Basics.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Disposables;

namespace FEx.Basics.Collections.Concurrent;

/// <summary>
///     Provides a thread-safe, concurrent collection for use with data binding.
///     Based on https://github.com/ChadBurggraf/parallel-extensions-extras
/// </summary>
/// <typeparam name="T">Specifies the type of the elements in this collection.</typeparam>
/// <seealso cref="System.Collections.Specialized.INotifyCollectionChanged" />
/// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[DebuggerTypeProxy(typeof(ListDebugView<>))]
[Serializable]
public class ObservableConcurrentCollection<T> : ConcurrentCollection<T>, IChangeableCollection,
    IObservableCollection<T>
{
    /// <summary>
    ///     Event raised when the collection changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler CollectionChanged
    {
        add => Notifier.CollectionChanged += value;
        remove => Notifier.CollectionChanged -= value;
    }

    /// <summary>
    ///     Event raised when a property on the collection changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged
    {
        add => Notifier.PropertyChanged += value;
        remove => Notifier.PropertyChanged -= value;
    }

    /// <summary>
    ///     Initializes an instance of the ObservableConcurrentCollection class with the specified
    ///     collection as the underlying data structure.
    /// </summary>
    public ObservableConcurrentCollection(IEnumerable<T> collection = null,
                                          bool useBaseConstructor = true,
                                          bool notifyOnCreationContext = false,
                                          bool passIndexOfRemovedItem = false,
                                          bool useResetOnBulkOperations = true,
                                          bool sendAsyncEvents = true)
        : base(collection, useBaseConstructor, passIndexOfRemovedItem, useResetOnBulkOperations, sendAsyncEvents)
    {
        SetNotifyOnCreationContext(notifyOnCreationContext);
    }

    /// <summary>
    ///     For serialization purposes
    /// </summary>
    public ObservableConcurrentCollection()
        : this(notifyOnCreationContext: false)
    {
    }

    /// <summary>
    ///     Suspends count notifications.
    /// </summary>
    /// <returns>A disposable when disposed will reset the count.</returns>
    public IDisposable SuspendCount()
    {
        SupressEvents();

        return Disposable.Create(ResumeEvents);
    }

    /// <summary>
    ///     Suspends notifications. When disposed, a reset notification is fired.
    /// </summary>
    /// <returns>A disposable when disposed will reset notifications.</returns>
    public IDisposable SuspendNotifications()
    {
        SupressEvents();

        return Disposable.Create(ResumeEvents);
    }

    /// <summary>
    ///     Clears the list and Loads the specified items.
    /// </summary>
    /// <param name="items">The items.</param>
    public void Load(IEnumerable<T> items) => ReplaceRange(items);

    /// <summary>Moves the item at the specified index to a new location in the collection.</summary>
    /// <param name="oldIndex">The zero-based index specifying the location of the item to be moved.</param>
    /// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
    public void Move(int oldIndex, int newIndex)
    {
        T obj = default;

        DoBulkOperation(coll =>
            {
                obj = coll[oldIndex];
                coll.RemoveAt(oldIndex);
                coll.Insert(newIndex, obj);
            },
            _ => false);

        OnCollectionChanged(NotifyCollectionChangedAction.Move,
            obj,
            newIndex,
            oldIndex,
            propertyChangedArgs: ["Item[]"]);
    }
}