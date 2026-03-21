using FEx.Agnostics.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace FEx.Core.Collections.Concurrent;

[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
[Serializable]
public class ConcurrentSortableObservableList<T> : ConcurrentObservableList<T> where T : IComparable<T>
{
    public ConcurrentSortableObservableList()
        : this(null)
    {
    }

    public ConcurrentSortableObservableList(IEnumerable<T> collection)
        : base(collection)
    {
    }

    public void Sort(ListSortDirection order) =>
        Write(() =>
        {
            if (order == ListSortDirection.Ascending)
                Items.Sort(static (a, b) => a.CompareTo(b));
            else
                Items.Sort(static (a, b) => -1 * a.CompareTo(b));
        });
}