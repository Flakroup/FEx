using FEx.Basics.Abstractions.Interfaces;
using FEx.Basics.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace FEx.Basics.Collections.Concurrent;

[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[Serializable]
public class ConcurrentList<T> : IList<T>, IReadOnlyList<T>, IList, ISuppressEvents
{
#pragma warning disable IDISP006
    [NonSerialized] protected readonly ExtendedReaderWriterLockSlim _lock;
#pragma warning restore IDISP006

    public int SuppressedEvents { get; set; }

    public bool EventsAreSuppressed => SuppressedEvents > 0;

    public bool IsSynchronized => ((ICollection)Items).IsSynchronized;
    public bool IsFixedSize => ((IList)Items).IsFixedSize;

    public object SyncRoot => Items;

    public int Count => Read(() => Items.Count);

    public bool IsEmpty => Count == 0;

    public bool IsReadOnly => ((IList)Items).IsReadOnly;

    public T this[int index]
    {
        get => Read(() => Items[index]);
        set => WriteWithResult(() => SetItem(index, value));
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected List<T> Items { get; }

    object IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T)value;
    }

    public ConcurrentList(IEnumerable<T> collection = null)
    {
        Items = [];
        _lock = new ExtendedReaderWriterLockSlim();

        var items = collection?.ToList();

        if (items?.Count > 0)
            AddRange(items);
    }

    public void CopyTo(Array array, int index) => Read(() => ((ICollection)Items).CopyTo(array, index));

    /// <summary>
    ///     Adds an object to the end of the <see cref="ConcurrentList{T}" />.
    /// </summary>
    /// <param name="item">
    ///     The object to be added to the end of the <see cref="ConcurrentList{T}" />.
    ///     The value can be null for reference types
    /// </param>
    public void Add(T item) => Write(() =>
    {
        int index = Items.Count;
        Items.Add(item);

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    });

    public void Clear() => Write(() =>
    {
        if (Items.Count == 0)
            return;

        Items.Clear();

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionReset();
    });

    public bool Contains(T item) => Read(() => Items.Contains(item));

    public void CopyTo(T[] array, int arrayIndex) => Read(() => Items.CopyTo(array, arrayIndex));

    /// <summary>
    ///     Removes the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    public bool Remove(T item) => WriteWithResult(() =>
    {
        int index = Items.IndexOf(item);

        if (index < 0)
            return false;

        RemoveAt(index);

        return true;
    });

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<T> GetEnumerator() => Read(Items.GetEnumerator);

    public int Add(object value)
    {
        var item = (T)value;

        return WriteWithResult(() =>
        {
            Add(item);

            return Items.Count;
        });
    }

    public bool Contains(object value) => Contains((T)value);

    public int IndexOf(object value) => IndexOf((T)value);

    public void Insert(int index, object value) => Insert(index, (T)value);

    public void Remove(object value) => Remove((T)value);

    public int IndexOf(T item) => Read(() => Items.IndexOf(item));

    public void Insert(int index, T item) => Write(() =>
    {
        Items.Insert(index, item);

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    });

    public void RemoveAt(int index) => Write(() =>
    {
        T removedItem = Items[index];
        Items.RemoveAt(index);

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();

        OnCollectionChanged(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index));
    });

    public SuppressEventsDisposable SuppressEvents() => new(this);

    /// <summary>
    ///     Adds the specified items to this collection.
    /// </summary>
    /// <param name="range">The items collection to add</param>
    public void AddRange(IEnumerable<T> range) => WriteWithResult(() => InternalAddRange(range));

    public ReadOnlyCollection<T> AsReadOnly() => Read(Items.AsReadOnly);

    /// <summary>
    ///     Adds an object to the end of the <see cref="ConcurrentList{T}" /> if it not exists in it yet.
    /// </summary>
    /// <param name="item">
    ///     The object to be added to the end of the <see cref="ConcurrentList{T}" />.
    ///     The value can be null for reference types
    /// </param>
    public bool AddUnique(T item) => WriteWithResult(() =>
    {
        if (Items.Contains(item))
            return false;

        Add(item);

        return true;
    });

    public void AddUniqueRange(IEnumerable<T> range) =>
        WriteWithResult(() => InternalAddRange(range.Distinct().Where(x => !Items.Contains(x))));

    public bool RemoveWhere(Func<T, bool> predicate)
    {
        var hasRemovedAny = false;

        Write(() =>
        {
            for (int i = Items.Count - 1; i > -1; i--)
            {
                T item = Items[i];

                if (predicate(item))
                {
                    RemoveAt(i);
                    hasRemovedAny = true;
                }
            }
        });

        return hasRemovedAny;
    }

    public void Replace(int index, T item) => WriteWithResult(() => SetItem(index, item));

    public void ReplaceWith(IEnumerable<T> collection)
    {
        var items = collection.ToList();

        Write(() =>
        {
            if (items.SequenceEqual(Items))
                return;

            using (SuppressEvents())
            {
                Clear();
                InternalAddRange(items);
            }

            OnCountPropertyChanged();
            OnIndexerPropertyChanged();
            OnCollectionReset();
        });
    }

    /// <summary>
    ///     Sorts the elements using the default comparer.
    /// </summary>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The default comparer
    /// <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find an implementation of the
    /// <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for
    ///     type <typeparamref name="T" />.
    /// </exception>
    public void Sort() => Write(() =>
    {
        Items.Sort();

        OnIndexerPropertyChanged();
        OnCollectionReset();
    });

    /// <summary>
    ///     Sorts the elements using the specified comparer.
    /// </summary>
    /// <param name="comparer">
    ///     The <see cref="T:System.Collections.Generic.IComparer`1" /> implementation to use when comparing
    ///     elements, or null to use the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" />.
    /// </param>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="comparer" /> is null, and the default comparer
    /// <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find implementation of the
    /// <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for
    ///     type <typeparamref name="T" />.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The implementation of <paramref name="comparer" /> caused an error during
    ///     the sort. For example, <paramref name="comparer" /> might not return 0 when comparing an item with itself.
    /// </exception>
    public void Sort(IComparer<T> comparer) => Write(() =>
    {
        Items.Sort(comparer);

        OnIndexerPropertyChanged();
        OnCollectionReset();
    });

    /// <summary>
    ///     Sorts the elements in a range of elements using the specified comparer.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range to sort.</param>
    /// <param name="count">The length of the range to sort.</param>
    /// <param name="comparer">
    ///     The <see cref="T:System.Collections.Generic.IComparer`1" /> implementation to use when comparing
    ///     elements, or null to use the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" />.
    /// </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///     <paramref name="index" /> is less than 0.-or-
    /// <paramref name="count" /> is less than 0.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="index" /> and <paramref name="count" /> do not specify a
    ///     valid range in the <see cref="T:System.Collections.Generic.List`1" />.-or-The implementation of
    /// <paramref name="comparer" /> caused an error during the sort. For example, <paramref name="comparer" /> might not
    ///     return 0 when comparing an item with itself.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="comparer" /> is null, and the default comparer
    /// <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find implementation of the
    /// <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for
    ///     type <typeparamref name="T" />.
    /// </exception>
    public void Sort(int index, int count, IComparer<T> comparer) => Write(() =>
    {
        Items.Sort(index, count, comparer);

        OnIndexerPropertyChanged();
        OnCollectionReset();
    });

    /// <summary>Sorts the elements using the specified <see cref="T:System.Comparison`1" />.</summary>
    /// <param name="comparison">The <see cref="T:System.Comparison`1" /> to use when comparing elements.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="comparison" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The implementation of <paramref name="comparison" /> caused an error
    ///     during the sort. For example, <paramref name="comparison" /> might not return 0 when comparing an item with itself.
    /// </exception>
    public void Sort(Comparison<T> comparison) => Write(() =>
    {
        Items.Sort(comparison);

        OnIndexerPropertyChanged();
        OnCollectionReset();
    });

    public void SortBy<TKey>(Func<T, TKey> selector,
                             ListSortDirection order = ListSortDirection.Ascending,
                             IComparer<TKey> comparer = null) => Write(() =>
    {
        var sortedItems = (order == ListSortDirection.Ascending
            ? Items.OrderBy(selector, comparer)
            : Items.OrderByDescending(selector, comparer)).ToList();

        using (SuppressEvents())
        {
            for (var i = 0; i < sortedItems.Count; i++)
                Items[i] = sortedItems[i];
        }

        OnIndexerPropertyChanged();
        OnCollectionReset();
    });

    /// <summary>
    ///     Suppresses all events regarding this collection while executing the specified action.
    /// <see cref="NotifyCollectionChangedAction.Reset" /> event is fired afterwards.
    /// </summary>
    /// <param name="action">The action.</param>
    public void Combo(Action action) => Write(() =>
    {
        using (SuppressEvents())
            action();

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionReset();
    });

    protected virtual void OnCountPropertyChanged()
    {
    }

    protected virtual void OnIndexerPropertyChanged()
    {
    }

    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
    }

    protected virtual void OnCollectionReset()
    {
    }

    protected void Read(Action action) => _lock.Read(action);

    protected TResult Read<TResult>(Func<TResult> action) => _lock.ReadWithResult(action);

    protected void Write(Action action) => _lock.Write(action);

    protected TResult WriteWithResult<TResult>(Func<TResult> action) => _lock.WriteWithResult(action);

    protected T SetItem(int index, T item)
    {
        T originalItem = this[index];
        Items[index] = item;

        OnIndexerPropertyChanged();

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, originalItem,
            item, index));

        return originalItem;
    }

    private (List<T> added, int startingIndex) InternalAddRange(IEnumerable<T> collection)
    {
        int startingIndex = Items.Count;
        var itemsToAdd = collection.ToList();

        Items.AddRange(itemsToAdd);

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();

        OnCollectionChanged(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, itemsToAdd, startingIndex));

        return (itemsToAdd, startingIndex);
    }
}