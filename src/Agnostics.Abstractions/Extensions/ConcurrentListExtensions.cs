using FEx.Agnostics.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class ConcurrentListExtensions
{
    public static void AddUniqueRange<T, TKey>(this IConcurrentList<T> list,
                                               IEnumerable<T> range,
                                               Func<T, TKey> keySelector) =>
        list.AddUniqueRange(range, keySelector, null);

    public static void SortBy<T, TKey>(this IConcurrentList<T> list,
                                       Func<T, TKey> selector) =>
        list.SortBy(selector, ListSortDirection.Ascending, null);

    public static void SortBy<T, TKey>(this IConcurrentList<T> list,
                                       Func<T, TKey> selector,
                                       ListSortDirection order) =>
        list.SortBy(selector, order, null);

    public static void Combo<T>(this IConcurrentList<T> list,
                                Action<IConcurrentList<T>> action) =>
        list.Combo(action, false);
}
