using FEx.Abstractions;
using FEx.Basics.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Basics.Extensions;

/// <summary>
///     IEnumerable interface extensions.
/// </summary>
public static class EnumerableExtensions
{
    public static IOrderedEnumerable<string> OrderAlphanumBy(this IEnumerable<string> source) =>
        source.OrderAlphanumBy(x => x);

    public static IOrderedEnumerable<TSource> OrderAlphanumBy<TSource>(this IEnumerable<TSource> source,
                                                                       Func<TSource, string> keySelector) =>
        source.OrderBy(keySelector, FExFoundation.AlphanumComparatorFast);

    public static IOrderedEnumerable<string> OrderAlphanumByDescending(this IEnumerable<string> source) =>
        source.OrderAlphanumByDescending(x => x);

    public static IOrderedEnumerable<TSource> OrderAlphanumByDescending<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, string> keySelector) =>
        source.OrderByDescending(keySelector, FExFoundation.AlphanumComparatorFast);

    public static ConcurrentObservableList<T> ToConcurrentObservableList<T>(this IEnumerable<T> source) => new(source);

    public static ConcurrentSortableObservableList<T> ToConcurrentSortableObservableList<T>(this IEnumerable<T> source)
        where T : IComparable<T> =>
        new(source);

    public static ConcurrentList<T> ToConcurrentList<T>(this IEnumerable<T> source) => new(source);

    public static ConcurrentSortableList<T> ToConcurrentSortableList<T>(this IEnumerable<T> source)
        where T : IComparable<T> =>
        new(source);
}