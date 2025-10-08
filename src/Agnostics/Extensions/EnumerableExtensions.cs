using FEx.Agnostics.Collections.Concurrent;
using FEx.Agnostics.Comparers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Agnostics.Extensions;

public static class EnumerableExtensions
{
    public static IOrderedEnumerable<string> OrderAlphanumBy(this IEnumerable<string> source) =>
        source.OrderAlphanumBy(x => x);

    public static IOrderedEnumerable<TSource> OrderAlphanumBy<TSource>(this IEnumerable<TSource> source,
                                                                       Func<TSource, string> keySelector) =>
        source.OrderBy(keySelector, new AlphanumComparatorFast());

    public static IOrderedEnumerable<string> OrderAlphanumByDescending(this IEnumerable<string> source) =>
        source.OrderAlphanumByDescending(x => x);

    public static IOrderedEnumerable<TSource> OrderAlphanumByDescending<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, string> keySelector) =>
        source.OrderByDescending(keySelector, new AlphanumComparatorFast());

    public static ConcurrentList<T> ToConcurrentList<T>(this IEnumerable<T> source) => new(source);

    public static ConcurrentSortableList<T> ToConcurrentSortableList<T>(this IEnumerable<T> source)
        where T : IComparable<T> =>
        new(source);
}