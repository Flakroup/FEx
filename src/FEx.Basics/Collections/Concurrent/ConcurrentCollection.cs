using FEx.Basics.Abstractions.Collections.Concurrent;
using FEx.Basics.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace FEx.Basics.Collections.Concurrent;

[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[DebuggerTypeProxy(typeof(ListDebugView<>))]
[Serializable]
public class ConcurrentCollection<T> : BaseConcurrentCollection<Collection<T>, T>
{
    public ConcurrentCollection(IEnumerable<T> collection = null,
                                bool useBaseConstructor = true,
                                bool passIndexOfRemovedItem = false,
                                bool useResetOnBulkOperations = true,
                                bool sendAsyncEvents = true)
        : base(collection, useBaseConstructor, passIndexOfRemovedItem, useResetOnBulkOperations, sendAsyncEvents)
    {
    }

    public ConcurrentCollection()
        : this(null)
    {
    }
}