using FEx.Agnostics.Abstractions.Collections.Concurrent;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
#if !NET6_0_OR_GREATER
using FEx.Agnostics.Abstractions.Extensions.Interop;
#endif

namespace FEx.Agnostics.Collections.Concurrent;

/// <summary>A thread-safe list that guards all access with a reader-writer lock and raises collection and property change notifications.</summary>
/// <typeparam name="T">The type of the list elements.</typeparam>
[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
[Serializable]
public partial class ConcurrentList<T> : BaseConcurrentList<T>, IConcurrentList<T>
{
    /// <summary>The lock that serializes writes and allows concurrent reads of <see cref="Items"/>.</summary>
    [NonSerialized]
    protected readonly ExtendedReaderWriterLockSlim _lock;

    /// <summary>Gets the number of elements in the list, read under the read lock.</summary>
    public int Count => Read(() => Items.Count);

    /// <summary>Gets a value indicating whether the list contains no elements.</summary>
    public bool IsEmpty => Count == 0;

    /// <summary>Gets or sets the element at the given index under the appropriate lock; setting raises replace notifications.</summary>
    /// <param name="index">The zero-based index of the element.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public T this[int index]
    {
        get => Read(() => Items[index]);
        set => SetItem(index, value);
    }

    /// <summary>Gets the underlying list; callers must hold the appropriate lock when using it.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected List<T> Items { get; }

    /// <summary>Initializes an empty list.</summary>
    public ConcurrentList()
        : this(null)
    {
    }

    /// <summary>Initializes the list with a snapshot of the given elements.</summary>
    /// <param name="collection">The initial elements, or <see langword="null"/> for an empty list.</param>
    public ConcurrentList(IEnumerable<T>? collection)
    {
        Items = [];
        _lock = new(this);

        var items = collection?.ToList();

        if (items?.Count > 0)
            AddRange(items);
    }

    /// <inheritdoc cref="List{T}.Add" />
    public void Add(T item) => AddCoreWithEvents(item);

    /// <inheritdoc cref="List{T}.Clear" />
    public void Clear()
    {
        Write(() =>
        {
            if (Items.Count == 0)
                return;

            Items.Clear();
        });

        WhenCollectionHasBeenReset();
    }

    /// <inheritdoc cref="List{T}.Contains" />
    public bool Contains(T item) => Read(() => Items.Contains(item));

    /// <inheritdoc cref="List{T}.CopyTo(T[])" />
    public void CopyTo(T[] array, int arrayIndex) => Read(() => Items.CopyTo(array, arrayIndex));

    /// <inheritdoc cref="List{T}.Remove" />
    public bool Remove(T item)
    {
        var (index, itemHasBeenRemoved) = Write(() =>
        {
            var index = Items.IndexOf(item);

            if (index == -1)
                return (-1, false);

            RemoveAtCore(index);

            return (index, true);
        });

        if (!itemHasBeenRemoved)
            return false;

        WhenItemIsRemoved(index, item);

        return true;
    }

    /// <inheritdoc />
    public void AddRange(IEnumerable<T> collection)
    {
        var (startingIndex, itemsToAdd) = Write(() =>
        {
            var itemsToAdd = collection?.ToList() ?? [];

            if (itemsToAdd.IsNullOrEmpty())
                return (-1, itemsToAdd);

            var count = Items.Count;
            Items.AddRange(itemsToAdd);

            return (count, itemsToAdd);
        });

        WhenRangeHasBeenAdded(startingIndex, itemsToAdd);
    }

    /// <inheritdoc cref="List{T}.AsReadOnly" />
    public ReadOnlyCollection<T> AsReadOnly() => Read(Items.AsReadOnly);

    /// <inheritdoc />
    public bool AddUnique(T item)
    {
        var index = Write(() =>
        {
            if (Items.Contains(item))
                return -1;

            var count = Items.Count;
            Items.Add(item);

            return count;
        });

        return WhenItemHasBeenAdded(item, index);
    }

    /// <inheritdoc />
    public void AddUniqueRange(IEnumerable<T> range) => AddRange(range.Distinct().Where(x => !Items.Contains(x)));

    /// <inheritdoc />
    public void
        AddUniqueRange<TKey>(IEnumerable<T> range, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer) =>
        AddRange([.. range.DistinctBy(keySelector, comparer)
            .Where(distinctItem => Items.All(item =>
                !comparer?.Equals(keySelector(distinctItem), keySelector(item))
                // Comparer-less path assumes non-null keys (existing runtime contract).
                ?? !keySelector(distinctItem)!.Equals(keySelector(item))))]);

    /// <inheritdoc />
    public bool RemoveWhere(Func<T, bool> predicate, out List<T> removedItems)
    {
        var innerRemovedItems = Write(() =>
        {
            List<(int index, T removedItem)> removedItems = [];

            for (var i = Items.Count - 1; i > -1; i--)
            {
                if (predicate(Items[i]))
                    removedItems.Add((i, RemoveAtCore(i)));
            }

            return removedItems;
        });

        foreach (var (index, removedItem) in innerRemovedItems)
            WhenItemIsRemoved(index, removedItem);

        removedItems = [.. innerRemovedItems.Select(static tuple => tuple.removedItem)];

        return !removedItems.IsNullOrEmpty();
    }

    /// <inheritdoc />
    public void Replace(int index, T item) => SetItem(index, item);

    /// <inheritdoc />
    public void ReplaceWith(IEnumerable<T> collection)
    {
        var items = collection.ToList();

        Combo(_ =>
        {
            if (items.SequenceEqual(Items))
                return false;

            Clear();
            AddRange(items);

            return true;
        });
    }

    /// <inheritdoc />
    public void Sort()
    {
        Write(Items.Sort);
        WhenCollectionHasBeenReordered();
    }

    /// <inheritdoc />
    public void Sort(IComparer<T> comparer)
    {
        Write(() => Items.Sort(comparer));
        WhenCollectionHasBeenReordered();
    }

    /// <inheritdoc />
    public void Sort(int index, int count, IComparer<T> comparer)
    {
        Write(() => Items.Sort(index, count, comparer));
        WhenCollectionHasBeenReordered();
    }

    /// <inheritdoc />
    public void Sort(Comparison<T> comparison)
    {
        Write(() => Items.Sort(comparison));
        WhenCollectionHasBeenReordered();
    }

    /// <inheritdoc />
    public void SortBy<TKey>(Func<T, TKey> selector, ListSortDirection order, IComparer<TKey>? comparer)
    {
        Write(() =>
        {
            var sortedItems = (order == ListSortDirection.Ascending
                ? Items.OrderBy(selector, comparer)
                : Items.OrderByDescending(selector, comparer)).ToList();

            using (SuppressEvents())
            {
                for (var i = 0; i < sortedItems.Count; i++)
                    Items[i] = sortedItems[i];
            }
        });

        WhenCollectionHasBeenReordered();
    }

    /// <inheritdoc />
    public void Combo(Action<IConcurrentList<T>> action, bool shouldTriggerCollectionReset) =>
        Combo(items =>
        {
            action(items);

            return shouldTriggerCollectionReset;
        });

    /// <inheritdoc />
    public bool Combo(Func<IConcurrentList<T>, bool> shouldTriggerCollectionReset)
    {
        var triggerCollectionReset = Write(() =>
        {
            using (SuppressEvents())
                return shouldTriggerCollectionReset(this);
        });

        if (!triggerCollectionReset)
            return false;

        WhenCollectionHasBeenReset();

        return true;
    }

    /// <inheritdoc />
    public void Move(int oldIndex, int newIndex) => MoveItem(oldIndex, newIndex);

    /// <inheritdoc cref="List{T}.GetEnumerator" />
    public IEnumerator<T> GetEnumerator() => Read(Items.GetEnumerator);

    /// <inheritdoc cref="List{T}.Add" />
    // IList.Add: value is unboxed to T; null into a value-type list throws, matching IList semantics.
    public int Add(object? value) => AddCoreWithEvents((T)value!);

    /// <inheritdoc cref="List{T}.IndexOf(T)" />
    public int IndexOf(T item) => Read(() => Items.IndexOf(item));

    /// <inheritdoc cref="List{T}.Insert" />
    public void Insert(int index, T item)
    {
        Write(() => Items.Insert(index, item));
        WhenItemIsInserted(index, item);
    }

    /// <inheritdoc cref="List{T}.RemoveAt" />
    public void RemoveAt(int index)
    {
        var removedItem = RemoveAtCore(index);
        WhenItemIsRemoved(index, removedItem);
    }

    /// <inheritdoc />
    public void AddUniqueRange<TKey>(IEnumerable<T> range, Func<T, TKey> keySelector) =>
        AddUniqueRange(range, keySelector, null);

    /// <inheritdoc />
    public void SortBy<TKey>(Func<T, TKey> selector) => SortBy(selector, ListSortDirection.Ascending, null);

    /// <summary>Sorts the list in place by the selected key using the default key comparer.</summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="selector">Selects the sort key from each element.</param>
    /// <param name="order">Whether to sort ascending or descending.</param>
    public void SortBy<TKey>(Func<T, TKey> selector, ListSortDirection order) => SortBy(selector, order, null);

    /// <inheritdoc />
    public void Combo(Action<IConcurrentList<T>> action) => Combo(action, false);

    /// <summary>Moves an element to a new position under the write lock and raises move notifications.</summary>
    /// <param name="oldIndex">The current index of the element.</param>
    /// <param name="newIndex">The index to move the element to.</param>
    protected virtual void MoveItem(int oldIndex, int newIndex)
    {
        var movedItem = Write(() =>
        {
            var movedItem = this[oldIndex];

            using (SuppressEvents())
            {
                RemoveAt(oldIndex);
                Insert(newIndex, movedItem);
            }

            return movedItem;
        });

        WhenItemIsMoved(oldIndex, newIndex, movedItem);
    }

    /// <summary>Replaces the element at the index under the write lock and raises replace notifications.</summary>
    /// <param name="index">The zero-based index of the element to replace.</param>
    /// <param name="item">The new element.</param>
    /// <returns>The element that was replaced.</returns>
    protected T SetItem(int index, T item)
    {
        var replacedItem = Write(() =>
        {
            var replacedItem = Items[index];
            Items[index] = item;

            return replacedItem;
        });

        WhenItemIsReplaced(index, item, replacedItem);

        return replacedItem;
    }

    /// <summary>Raises the indexer and collection-reset notifications after the elements have been reordered.</summary>
    protected void WhenCollectionHasBeenReordered()
    {
        OnIndexerPropertyChanged();
        OnCollectionReset();
    }

    private T RemoveAtCore(int index) =>
        Write(() =>
        {
            var removedItem = Items[index];
            Items.RemoveAt(index);

            return removedItem;
        });

    private int AddCoreWithEvents(T item)
    {
        var index = Write(() =>
        {
            var count = Items.Count;
            Items.Add(item);

            return count;
        });

        WhenItemHasBeenAdded(item, index);

        return index;
    }

    private bool WhenItemHasBeenAdded(T item, int index)
    {
        if (index == -1)
            return false;

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnAddToCollection(item, index);

        return true;
    }

    private void WhenRangeHasBeenAdded(int startingIndex, List<T> itemsToAdd)
    {
        if (startingIndex == -1)
            return;

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();

        if (itemsToAdd.Count >= startingIndex / 5)
        {
            OnCollectionReset();

            return;
        }

        for (var i = 0; i < itemsToAdd.Count; i++)
            OnAddToCollection(itemsToAdd[i], startingIndex + i);
    }

    private void WhenCollectionHasBeenReset()
    {
        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionReset();
    }

    private void WhenItemIsInserted(int index, T item)
    {
        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnAddToCollection(item, index);
    }

    private void WhenItemIsRemoved(int index, T removedItem)
    {
        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnRemoveFromCollection(removedItem, index);
    }

    private void WhenItemIsMoved(int oldIndex, int newIndex, T removedItem)
    {
        OnIndexerPropertyChanged();
        OnMoveInCollection(removedItem, newIndex, oldIndex);
    }

    private void WhenItemIsReplaced(int index, T item, T replacedItem)
    {
        OnIndexerPropertyChanged();
        OnReplaceInCollection(replacedItem, item, index);
    }
}