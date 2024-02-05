using FEx.Basics.Abstractions.Interfaces.Collections;
using FEx.Basics.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace FEx.Basics.Collections.Concurrent;

/// <summary>
///     Provides a thread-safe, concurrent collection for use with data binding.
///     Based on https://github.com/ChadBurggraf/parallel-extensions-extras
/// </summary>
/// <typeparam name="T">Specifies the type of the elements in this collection.</typeparam>
[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[DebuggerTypeProxy(typeof(ListDebugView<>))]
[Serializable]
public class ObservableSortableConcurrentCollection<T> : ConcurrentSortableCollection<T>, IChangeableCollection
    where T : IComparable<T>
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
    public ObservableSortableConcurrentCollection(IEnumerable<T> collection = null,
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
    public ObservableSortableConcurrentCollection()
        : this(notifyOnCreationContext: false)
    {
    }
}