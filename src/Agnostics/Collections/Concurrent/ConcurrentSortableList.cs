using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace FEx.Agnostics.Collections.Concurrent;

[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
[Serializable]
public class ConcurrentSortableList<T> : ConcurrentList<T> where T : IComparable<T>
{
    public ConcurrentSortableList(IEnumerable<T> collection = null)
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