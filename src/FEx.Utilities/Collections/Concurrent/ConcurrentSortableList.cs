using System;
using System.ComponentModel;

namespace FEx.Utilities.Collections.Concurrent;

public class ConcurrentSortableList<T> : ConcurrentList<T> where T : IComparable<T>
{
    public void Sort(ListSortDirection order)
    {
        Write(() =>
        {
            if (order == ListSortDirection.Ascending)
                Items.Sort((a, b) => a.CompareTo(b));
            else
                Items.Sort((a, b) => -1 * a.CompareTo(b));
        });
    }
}