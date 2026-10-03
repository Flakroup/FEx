using FEx.Agnostics.Collections.Concurrent;
using FEx.Agnostics.Comparers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Agnostics.Extensions;

/// <summary>Contains LINQ-style extension methods for natural-order sorting and for creating concurrent lists.</summary>
public static class EnumerableExtensions
{
    /// <summary>Sorts strings in ascending natural order, where digit runs compare numerically.</summary>
    /// <param name="source">The sequence to sort.</param>
    /// <returns>An ordered sequence of the strings.</returns>
    public static IOrderedEnumerable<string> OrderAlphanumBy(this IEnumerable<string> source) =>
        source.OrderAlphanumBy(x => x);

    /// <summary>Sorts elements in ascending natural order of a string key, where digit runs compare numerically.</summary>
    /// <typeparam name="TSource">The type of the elements.</typeparam>
    /// <param name="source">The sequence to sort.</param>
    /// <param name="keySelector">Selects the string key to sort by from each element.</param>
    /// <returns>An ordered sequence of the elements.</returns>
    public static IOrderedEnumerable<TSource> OrderAlphanumBy<TSource>(this IEnumerable<TSource> source,
                                                                       Func<TSource, string> keySelector) =>
        source.OrderBy(keySelector, new AlphanumComparatorFast());

    /// <summary>Sorts strings in descending natural order, where digit runs compare numerically.</summary>
    /// <param name="source">The sequence to sort.</param>
    /// <returns>An ordered sequence of the strings.</returns>
    public static IOrderedEnumerable<string> OrderAlphanumByDescending(this IEnumerable<string> source) =>
        source.OrderAlphanumByDescending(x => x);

    /// <summary>Sorts elements in descending natural order of a string key, where digit runs compare numerically.</summary>
    /// <typeparam name="TSource">The type of the elements.</typeparam>
    /// <param name="source">The sequence to sort.</param>
    /// <param name="keySelector">Selects the string key to sort by from each element.</param>
    /// <returns>An ordered sequence of the elements.</returns>
    public static IOrderedEnumerable<TSource> OrderAlphanumByDescending<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, string> keySelector) =>
        source.OrderByDescending(keySelector, new AlphanumComparatorFast());

    /// <summary>Copies a sequence into a new thread-safe list.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The sequence to copy.</param>
    /// <returns>A <see cref="ConcurrentList{T}"/> containing the elements.</returns>
    public static ConcurrentList<T> ToConcurrentList<T>(this IEnumerable<T> source) => new(source);

    /// <summary>Copies a sequence of comparable elements into a new thread-safe sortable list.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The sequence to copy.</param>
    /// <returns>A <see cref="ConcurrentSortableList{T}"/> containing the elements.</returns>
    public static ConcurrentSortableList<T> ToConcurrentSortableList<T>(this IEnumerable<T> source)
        where T : IComparable<T> =>
        new(source);
}