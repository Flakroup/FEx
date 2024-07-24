using FEx.Extensions.Collections.Lists;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Extensions.Collections;

public static class CollectionExtensions
{
    /// <summary>
    ///     Appends a sequence of items to an existing list
    /// </summary>
    /// <typeparam name="T">The type of the items in the list.</typeparam>
    /// <param name="source">The list to modify.</param>
    /// <param name="items">The sequence of items to add to the list.</param>
    public static void AddRangeToCollection<T>(this ICollection<T> source, IEnumerable<T> items)
    {
        if (items is null)
            return;

        source.AddRangeToCollection(items as T[] ?? items.ToArray());
    }

    /// <summary>
    ///     Appends a sequence of items to an existing list
    /// </summary>
    /// <typeparam name="T">The type of the items in the list.</typeparam>
    /// <param name="source">The list to modify.</param>
    /// <param name="items">The sequence of items to add to the list.</param>
    public static void AddRangeToCollection<T>(this ICollection<T> source, IList<T> items)
    {
        if (items.IsNotNullOrEmptyList())
            foreach (T item in items)
                source.Add(item);
    }

    public static void Remove<T>(this ICollection<T> items, T item) where T : class => items.Remove(item);

    /// <summary>
    ///     Adds the specified item many times.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">The items.</param>
    /// <param name="item">The item.</param>
    /// <param name="count">The count.</param>
    /// <param name="creator">The creator.</param>
    public static void Add<T>(this ICollection<T> items, T item, int count = 1, Func<T, T> creator = null)
        where T : class
    {
        for (var i = 0; i < count; i++)
        {
            items.Add(creator is not null
                ? creator(item)
                : item);
        }
    }

    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyReadOnlyCollection<T>(this IReadOnlyCollection<T> source) => source?.Count > 0;

    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmptyReadOnlyCollection<T>(this IReadOnlyCollection<T> source) =>
        source is null || source.Count == 0;
}