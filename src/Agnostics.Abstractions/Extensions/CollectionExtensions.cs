using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for counting, batching, flattening and modifying collections.</summary>
public static class CollectionExtensions
{
    /// <summary>Counts the elements of a non-generic sequence, using <see cref="System.Collections.ICollection.Count" /> when available</summary>
    /// <param name="source">The sequence to count.</param>
    /// <returns>The number of elements.</returns>
    public static int Count(this IEnumerable source)
    {
        if (source is ICollection collection)
            return collection.Count;

        return Enumerable.Count(source.Cast<object>());
    }

    /// <summary>Determines whether a collection is not null and has at least one element</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The collection to test.</param>
    /// <returns><c>true</c> if the collection has elements.</returns>
    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyCollection<T>(this ICollection<T> source) => source?.Count > 0;

    /// <summary>Determines whether a collection is null or has no elements</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The collection to test.</param>
    /// <returns><c>true</c> if the collection is null or empty.</returns>
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmptyCollection<T>(this ICollection<T> source) => source is null || source.Count == 0;

    /// <summary>Determines whether a read-only collection is not null and has at least one element</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The collection to test.</param>
    /// <returns><c>true</c> if the collection has elements.</returns>
    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyReadOnlyCollection<T>(this IReadOnlyCollection<T> source) => source?.Count > 0;

    /// <summary>Determines whether a read-only collection is null or has no elements</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The collection to test.</param>
    /// <returns><c>true</c> if the collection is null or empty.</returns>
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmptyReadOnlyCollection<T>(this IReadOnlyCollection<T> source) =>
        source is null || source.Count == 0;

    /// <summary>Splits a sequence into consecutive batches</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to split.</param>
    /// <param name="batchSize">The maximum number of elements per batch.</param>
    /// <returns>The batches; the last one may be smaller.</returns>
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

    /// <summary>Removes every element that matches a predicate</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The collection to modify.</param>
    /// <param name="predicate">Returns true for the elements to remove.</param>
    public static void Remove<T>(this ICollection<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source.Where(predicate).ToList())
            source.Remove(item);
    }

    /// <summary>Removes an item from a collection</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The collection to modify.</param>
    /// <param name="item">The item to remove.</param>
    public static void Remove<T>(this ICollection<T> items, T item) where T : class => items.Remove(item);

    /// <summary>
    /// Adds the specified item many times.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">The items.</param>
    /// <param name="item">The item.</param>
    /// <param name="count">The count.</param>
    /// <param name="creator">The creator.</param>
    public static void Add<T>(this ICollection<T> items, T item, int count = 1, Func<T, T>? creator = null)
        where T : class
    {
        for (var i = 0; i < count; i++)
        {
            items.Add(creator is not null
                ? creator(item)
                : item);
        }
    }

    /// <summary>Flattens a hierarchy into a single sequence of descendants followed by the root items</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="root">The top-level items.</param>
    /// <param name="predicate">Gets the children of an item.</param>
    /// <returns>All items of the hierarchy.</returns>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<T> root, Func<T, IEnumerable<T>> predicate)
    {
        var deferralList = root.ToList();

        return deferralList.SelectMany(x => predicate(x).Flatten(predicate)).Concat(deferralList);
    }

    /// <summary>Copies a sequence into a new <see cref="ObservableCollection{T}" /></summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to copy.</param>
    /// <returns>The new collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is null.</exception>
    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source), $"{nameof(source)} must not be null.");

        return [.. source];
    }

    /// <summary>Copies a sequence into a read-only list</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="collection">The sequence to copy.</param>
    /// <returns>A read-only list with the elements.</returns>
    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> collection) =>
        collection.ToList().AsReadOnly();

    /// <summary>Converts each element and returns the results as a read-only collection</summary>
    /// <typeparam name="T">The source element type.</typeparam>
    /// <typeparam name="TOut">The converted element type.</typeparam>
    /// <param name="items">The sequence to convert.</param>
    /// <param name="converter">Converts an element.</param>
    /// <param name="defaultValue">Returned when <paramref name="items" /> is null.</param>
    /// <returns>The converted elements, or <paramref name="defaultValue" /> when the sequence is null.</returns>
    public static IReadOnlyCollection<TOut>? ToReadOnlyCollectionOrDefault<T, TOut>(this IEnumerable<T> items,
        Func<T, TOut> converter,
        IReadOnlyCollection<TOut>? defaultValue = null) =>
        items?.Select(converter).ToReadOnlyList() ?? defaultValue;

    /// <summary>Moves an item to a new index within a list</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="list">The list to modify.</param>
    /// <param name="item">The item to move.</param>
    /// <param name="newIndex">The index to insert the item at after removal.</param>
    /// <exception cref="NullReferenceException">The list does not contain the item.</exception>
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