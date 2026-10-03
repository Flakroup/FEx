using FEx.Agnostics.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for <see cref="IConcurrentList{T}" /> that supply default arguments.</summary>
public static class ConcurrentListExtensions
{
    /// <summary>Adds the items whose key is not already used, comparing keys with the default equality</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="list">The list to add to.</param>
    /// <param name="range">The candidate items.</param>
    /// <param name="keySelector">Selects the key of an item.</param>
    public static void AddUniqueRange<T, TKey>(this IConcurrentList<T> list,
                                               IEnumerable<T> range,
                                               Func<T, TKey> keySelector) =>
        list.AddUniqueRange(range, keySelector, null);

    /// <summary>Sorts the list ascending by a key using the default key comparer</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="list">The list to sort.</param>
    /// <param name="selector">Selects the sort key of an item.</param>
    public static void SortBy<T, TKey>(this IConcurrentList<T> list, Func<T, TKey> selector) =>
        list.SortBy(selector, ListSortDirection.Ascending, null);

    /// <summary>Sorts the list by a key using the default key comparer</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="list">The list to sort.</param>
    /// <param name="selector">Selects the sort key of an item.</param>
    /// <param name="order">The sort direction.</param>
    public static void SortBy<T, TKey>(this IConcurrentList<T> list, Func<T, TKey> selector, ListSortDirection order) =>
        list.SortBy(selector, order, null);

    /// <summary>Runs an action with events suppressed and without raising a reset event afterwards</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="list">The list to operate on.</param>
    /// <param name="action">The action to run.</param>
    public static void Combo<T>(this IConcurrentList<T> list, Action<IConcurrentList<T>> action) =>
        list.Combo(action, false);
}