using FEx.Agnostics.Abstractions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>A thread-safe list that supports bulk operations, sorting and event suppression.</summary>
/// <typeparam name="T">The element type.</typeparam>
[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IConcurrentList<T> : IList<T>, IReadOnlyList<T>, IList, ISuppressEvents
{
    /// <summary>Gets the configuration of the collection events.</summary>
    CollectionEventsConfig Config { get; }
    /// <summary>Gets a value indicating whether the list contains no elements.</summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Adds the specified items to this collection.
    /// </summary>
    /// <param name="range">The items collection to add</param>
    void AddRange(IEnumerable<T> range);

    /// <summary>Returns a read-only wrapper around the list.</summary>
    /// <returns>A read-only view of the list.</returns>
    ReadOnlyCollection<T> AsReadOnly();

    /// <summary>
    /// Adds an object to the end of the <see cref="IConcurrentList{T}" /> if it not exists in it yet.
    /// </summary>
    /// <param name="item">
    /// The object to be added to the end of the <see cref="IConcurrentList{T}" />.
    /// The value can be null for reference types
    /// </param>
    bool AddUnique(T item);

    /// <summary>Adds the items that are not already in the list, ignoring duplicates within the range.</summary>
    /// <param name="range">The candidate items.</param>
    void AddUniqueRange(IEnumerable<T> range);
    /// <summary>Adds the items whose key is not already used by an element of the list, ignoring duplicate keys within the range.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="range">The candidate items.</param>
    /// <param name="keySelector">Selects the key of an item.</param>
    /// <param name="comparer">Compares keys; the default equality is used when null.</param>
    void AddUniqueRange<TKey>(IEnumerable<T> range, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer);
    /// <summary>Removes all items that match a predicate.</summary>
    /// <param name="predicate">Returns true for the items to remove.</param>
    /// <param name="removedItems">Receives the removed items.</param>
    /// <returns><c>true</c> if at least one item was removed.</returns>
    bool RemoveWhere(Func<T, bool> predicate, out List<T> removedItems);
    /// <summary>Replaces the item at an index.</summary>
    /// <param name="index">The zero-based index of the item to replace.</param>
    /// <param name="item">The new item.</param>
    void Replace(int index, T item);
    /// <summary>Replaces the content of the list, doing nothing when the sequence is already equal to the current content.</summary>
    /// <param name="collection">The new items.</param>
    void ReplaceWith(IEnumerable<T> collection);

    /// <summary>
    /// Sorts the elements using the default comparer.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The default comparer
    /// <see cref="System.Collections.Generic.Comparer{T}.Default" /> cannot find an implementation of the
    /// <see cref="IComparable{T}" /> generic interface or the <see cref="IComparable" /> interface for
    /// type <typeparamref name="T" />.
    /// </exception>
    void Sort();

    /// <summary>
    /// Sorts the elements using the specified comparer.
    /// </summary>
    /// <param name="comparer">
    /// The <see cref="System.Collections.Generic.IComparer{T}" /> implementation to use when comparing
    /// elements, or null to use the default comparer <see cref="System.Collections.Generic.Comparer{T}.Default" />.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="comparer" /> is null, and the default comparer
    /// <see cref="System.Collections.Generic.Comparer{T}.Default" /> cannot find implementation of the
    /// <see cref="IComparable{T}" /> generic interface or the <see cref="IComparable" /> interface for
    /// type <typeparamref name="T" />.
    /// </exception>
    /// <exception cref="ArgumentException">
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
    /// The <see cref="System.Collections.Generic.IComparer{T}" /> implementation to use when comparing
    /// elements, or null to use the default comparer <see cref="System.Collections.Generic.Comparer{T}.Default" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0.-or-
    /// <paramref name="count" /> is less than 0.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="index" /> and <paramref name="count" /> do not specify a
    /// valid range in the <see cref="System.Collections.Generic.List{T}" />.-or-The implementation of
    /// <paramref name="comparer" /> caused an error during the sort. For example, <paramref name="comparer" /> might not
    /// return 0 when comparing an item with itself.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="comparer" /> is null, and the default comparer
    /// <see cref="System.Collections.Generic.Comparer{T}.Default" /> cannot find implementation of the
    /// <see cref="IComparable{T}" /> generic interface or the <see cref="IComparable" /> interface for
    /// type <typeparamref name="T" />.
    /// </exception>
    void Sort(int index, int count, IComparer<T> comparer);

    /// <summary>Sorts the elements using the specified <see cref="System.Comparison{T}" />.</summary>
    /// <param name="comparison">The <see cref="System.Comparison{T}" /> to use when comparing elements.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="comparison" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The implementation of <paramref name="comparison" /> caused an error
    /// during the sort. For example, <paramref name="comparison" /> might not return 0 when comparing an item with itself.
    /// </exception>
    void Sort(Comparison<T> comparison);

    /// <summary>Sorts the list by a key.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="selector">Selects the sort key of an item.</param>
    /// <param name="order">The sort direction.</param>
    /// <param name="comparer">Compares keys; the default comparer is used when null.</param>
    void SortBy<TKey>(Func<T, TKey> selector, ListSortDirection order, IComparer<TKey>? comparer);

    /// <summary>
    /// Suppresses all events regarding this collection while executing the specified action.
    /// <see cref="System.Collections.Specialized.NotifyCollectionChangedAction.Reset" /> event is fired afterward.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="shouldTriggerCollectionReset">
    /// Boolean flag indicating whether collection reset event should be triggered
    /// or not
    /// </param>
    void Combo(Action<IConcurrentList<T>> action, bool shouldTriggerCollectionReset);

    /// <summary>Runs a function with events suppressed and raises a reset event when it asks for it.</summary>
    /// <param name="shouldTriggerCollectionReset">Receives the list and returns true when a reset event should be raised.</param>
    /// <returns><c>true</c> if a reset event was raised.</returns>
    bool Combo(Func<IConcurrentList<T>, bool> shouldTriggerCollectionReset);

    /// <summary>
    /// Move item at oldIndex to newIndex.
    /// </summary>
    void Move(int oldIndex, int newIndex);
}