using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class CollectionExtensions
{
    public static int Count(this IEnumerable source)
    {
        if (source is ICollection collection)
            return collection.Count;

        return Enumerable.Count(source.Cast<object>());
    }

    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyCollection<T>(this ICollection<T> source) => source?.Count > 0;

    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmptyCollection<T>(this ICollection<T> source) => source is null || source.Count == 0;

    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyReadOnlyCollection<T>(this IReadOnlyCollection<T> source) => source?.Count > 0;

    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmptyReadOnlyCollection<T>(this IReadOnlyCollection<T> source) =>
        source is null || source.Count == 0;

    public static IEnumerable<IEnumerable<T>> BatchBy<T>(this IEnumerable<T> source, int batchSize)
    {
        var list = source.ToList();

        for (var i = 0; i < list.Count; i += batchSize)
            yield return list.GetRange(i, Math.Min(batchSize, list.Count - i));
    }

    /// <summary>
    /// Appends a sequence of items to an existing collection
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
    /// <param name="source">The collection to modify.</param>
    /// <param name="items">The sequence of items to add to the collection.</param>
    public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
    {
        foreach (var item in items)
            source.Add(item);
    }

    /// <summary>
    /// Appends a sequence of items to an existing collection
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
    /// <param name="source">The collection to modify.</param>
    /// <param name="items">The sequence of items to add to the collection.</param>
    public static void AddRangeToCollection<T>(this ICollection<T> source, IEnumerable<T> items)
    {
        if (items is null)
            return;

        source.AddRangeToCollection(items as T[] ?? [.. items]);
    }

    /// <summary>
    /// Appends a sequence of items to an existing collection
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
    /// <param name="source">The collection to modify.</param>
    /// <param name="items">The sequence of items to add to the collection.</param>
    public static void AddRangeToCollection<T>(this ICollection<T> source, IList<T> items)
    {
        if (items.IsNotNullOrEmptyList())
            foreach (var item in items)
                source.Add(item);
    }

    public static void Remove<T>(this ICollection<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source.Where(predicate).ToList())
            source.Remove(item);
    }

    public static void Remove<T>(this ICollection<T> items, T item) where T : class => items.Remove(item);

    /// <summary>
    /// Adds the specified item many times.
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

    public static IEnumerable<T> Flatten<T>(this IEnumerable<T> root, Func<T, IEnumerable<T>> predicate)
    {
        var deferralList = root.ToList();

        return deferralList.SelectMany(x => predicate(x).Flatten(predicate)).Concat(deferralList);
    }

    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source), $"{nameof(source)} must not be null.");

        return [.. source];
    }

    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> collection) =>
        collection.ToList().AsReadOnly();

    public static IReadOnlyCollection<TOut> ToReadOnlyCollectionOrDefault<T, TOut>(this IEnumerable<T> items,
        Func<T, TOut> converter,
        IReadOnlyCollection<TOut> defaultValue = null) =>
        items?.Select(converter).ToReadOnlyList() ?? defaultValue;

    public static void Move<T>(this IList<T> list, T item, int newIndex)
    {
        item.Guard(nameof(item));

        var oldIndex = list.IndexOf(item);

        if (oldIndex == -1)
            throw new NullReferenceException("The list does not contain this item");

        list.RemoveAt(oldIndex);

        list.Insert(newIndex, item);
    }
}