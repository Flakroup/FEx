using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace FEx.Basics.Collections.Concurrent;

[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[Serializable]
public class ConcurrentSortableObservableList<T> : ConcurrentObservableList<T> where T : IComparable<T>
{
    public ConcurrentSortableObservableList(IEnumerable<T> collection = null)
        : base(collection)
    {
    }

    public void Sort(ListSortDirection order) =>
        Write(() =>
        {
            if (order == ListSortDirection.Ascending)
                Items.Sort((a, b) => a.CompareTo(b));
            else
                Items.Sort((a, b) => -1 * a.CompareTo(b));
        });
}