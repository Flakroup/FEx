using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace FEx.Agnostics.Collections.Concurrent;

/// <summary>A thread-safe list of comparable items that can be sorted in place.</summary>
/// <typeparam name="T">The type of the list elements.</typeparam>
[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
[Serializable]
public class ConcurrentSortableList<T> : ConcurrentList<T> where T : IComparable<T>
{
    /// <summary>Initializes an empty list.</summary>
    public ConcurrentSortableList()
        : this(null)
    {
    }

    /// <summary>Initializes the list with the elements of the given collection.</summary>
    /// <param name="collection">The initial elements, or <see langword="null"/> for an empty list.</param>
    public ConcurrentSortableList(IEnumerable<T>? collection)
        : base(collection)
    {
    }

    /// <summary>Sorts the list in place under the write lock and then signals that the collection was reordered.</summary>
    /// <param name="order">Whether to sort ascending or descending.</param>
    public void Sort(ListSortDirection order)
    {
        Write(() =>
        {
            if (order == ListSortDirection.Ascending)
                Items.Sort(static (a, b) => a.CompareTo(b));
            else
                Items.Sort(static (a, b) => -1 * a.CompareTo(b));
        });

        WhenCollectionHasBeenReordered();
    }
}