using FEx.Basics.Collections.Concurrent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Basics.Abstractions.Interfaces.Collections;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IConcurrentList<T> : IList<T>, IReadOnlyList<T>, IList, ISuppressEvents
{
    CollectionEventsConfig Config { get; }
    bool IsEmpty { get; }

    /// <summary>
    /// Adds the specified items to this collection.
    /// </summary>
    /// <param name="range">The items collection to add</param>
    void AddRange(IEnumerable<T> range);

    ReadOnlyCollection<T> AsReadOnly();

    /// <summary>
    /// Adds an object to the end of the <see cref="ConcurrentList{T}" /> if it not exists in it yet.
    /// </summary>
    /// <param name="item">
    /// The object to be added to the end of the <see cref="ConcurrentList{T}" />.
    /// The value can be null for reference types
    /// </param>
    bool AddUnique(T item);

    void AddUniqueRange(IEnumerable<T> range);
    void AddUniqueRange<TKey>(IEnumerable<T> range, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer = null);
    bool RemoveWhere(Func<T, bool> predicate, out List<T> removedItems);
    void Replace(int index, T item);
    void ReplaceWith(IEnumerable<T> collection);

    /// <summary>
    /// Sorts the elements using the default comparer.
    /// </summary>
    /// <exception cref="T:System.InvalidOperationException">
    /// The default comparer
    /// <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find an implementation of the
    /// <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for
    /// type <typeparamref name="T" />.
    /// </exception>
    void Sort();

    /// <summary>
    /// Sorts the elements using the specified comparer.
    /// </summary>
    /// <param name="comparer">
    /// The <see cref="T:System.Collections.Generic.IComparer`1" /> implementation to use when comparing
    /// elements, or null to use the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" />.
    /// </param>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="comparer" /> is null, and the default comparer
    /// <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find implementation of the
    /// <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for
    /// type <typeparamref name="T" />.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// The implementation of <paramref name="comparer" /> caused an error during
    /// the sort. For example, <paramref name="comparer" /> might not return 0 when comparing an item with itself.
    /// </exception>
    void Sort(IComparer<T> comparer);

    /// <summary>
    /// Sorts the elements in a range of elements using the specified comparer.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range to sort.</param>
    /// <param name="count">The length of the range to sort.</param>
    /// <param name="comparer">
    /// The <see cref="T:System.Collections.Generic.IComparer`1" /> implementation to use when comparing
    /// elements, or null to use the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" />.
    /// </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0.-or-
    /// <paramref name="count" /> is less than 0.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="index" /> and <paramref name="count" /> do not specify a
    /// valid range in the <see cref="T:System.Collections.Generic.List`1" />.-or-The implementation of
    /// <paramref name="comparer" /> caused an error during the sort. For example, <paramref name="comparer" /> might not
    /// return 0 when comparing an item with itself.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="comparer" /> is null, and the default comparer
    /// <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find implementation of the
    /// <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for
    /// type <typeparamref name="T" />.
    /// </exception>
    void Sort(int index, int count, IComparer<T> comparer);

    /// <summary>Sorts the elements using the specified <see cref="T:System.Comparison`1" />.</summary>
    /// <param name="comparison">The <see cref="T:System.Comparison`1" /> to use when comparing elements.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="comparison" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// The implementation of <paramref name="comparison" /> caused an error
    /// during the sort. For example, <paramref name="comparison" /> might not return 0 when comparing an item with itself.
    /// </exception>
    void Sort(Comparison<T> comparison);

    void SortBy<TKey>(Func<T, TKey> selector,
                      ListSortDirection order = ListSortDirection.Ascending,
                      IComparer<TKey> comparer = null);

    /// <summary>
    /// Suppresses all events regarding this collection while executing the specified action.
    /// <see cref="NotifyCollectionChangedAction.Reset" /> event is fired afterward.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="shouldTriggerCollectionReset">
    /// Boolean flag indicating whether collection reset event should be triggered
    /// or not
    /// </param>
    void Combo(Action<IConcurrentList<T>> action, bool shouldTriggerCollectionReset = false);

    bool Combo(Func<IConcurrentList<T>, bool> shouldTriggerCollectionReset);

    /// <summary>
    /// Move item at oldIndex to newIndex.
    /// </summary>
    void Move(int oldIndex, int newIndex);
}