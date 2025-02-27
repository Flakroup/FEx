using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FEx.Extensions.Collections.Enumerables;

/// <summary>
///     IEnumerable interface extensions.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    ///     Determines whether I'm null or empty.
    /// </summary>
    /// <typeparam name="T">The type of the items in the list.</typeparam>
    /// <param name="source">The source.</param>
    /// <returns>True, if source is null or contains no data; Otherwise false.</returns>
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmptyEnumerable<T>(this IEnumerable<T> source) => source?.Any() != true;

    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyEnumerable<T>(this IEnumerable<T> source) => source?.Any() == true;

    /// <summary>
    ///     Appends a sequence of items to an existing list
    /// </summary>
    /// <typeparam name="T">The type of the items in the list.</typeparam>
    /// <param name="source">The list to modify.</param>
    /// <param name="items">The sequence of items to add to the list.</param>
    /// <returns></returns>
    public static void AddRange<T>(ref IEnumerable<T> source, IEnumerable<T> items) => source = source.Concat(items);

    /// <summary>
    ///     Aggregates a list of strings.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>A comma separated string with values if any; Otherwise a empty string.</returns>
    public static string AggregateSafe(this IEnumerable<string> source)
    {
        IEnumerable<string> enumerable = source as string[] ?? [.. source];
        string result = string.Empty;

        if (enumerable.Any())
            result = string.Join(", ", enumerable);

        return result;
    }

    /// <summary>
    ///     Execute a action for each item in the list.
    /// </summary>
    /// <typeparam name="T">Sequence element type.</typeparam>
    /// <param name="source">The list itself.</param>
    /// <param name="action">Action to take on each item</param>
    /// <returns>The list itself.</returns>
    public static void ForEachInEnumerable<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (T item in source)
            action(item);
    }

    /// <summary>
    ///     Multiplies the items by given multiplier number.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">The items.</param>
    /// <param name="multiplier">The multiplier.</param>
    /// <returns>The items multiplied by given multiplier number.</returns>
    public static IEnumerable<T> MultiplyBy<T>(this IEnumerable<T> items, int multiplier)
    {
        var multipliedItems = new List<T>();

        for (var i = 0; i < multiplier; i++)
            multipliedItems.AddRange(items);

        return multipliedItems;
    }

    /// <summary>
    ///     Divides the specified set of items into sets of smaller ones (less than given maximum number of items).
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="items">The items.</param>
    /// <param name="maxNumberOfItems">The maximum number of items.</param>
    /// <returns>Sets of smaller ones (less than given maximum number of items).</returns>
    public static IEnumerable<IEnumerable<T>> Divide<T>(this IEnumerable<T> items, int maxNumberOfItems)
    {
        var dividedLists = new List<IEnumerable<T>>();
        var partialList = new List<T>();
        var counter = 0;

        foreach (T item in items)
        {
            if (counter == 0
                || counter % maxNumberOfItems == 0)
            {
                partialList = [];
                dividedLists.Add(partialList);
            }

            partialList.Add(item);
            counter++;
        }

        return dividedLists;
    }

    /// <summary>
    ///     Maximums the or default.
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="items">The items.</param>
    /// <param name="selector">The selector.</param>
    /// <returns></returns>
    public static TResult MaxOrDefault<TItem, TResult>(this IEnumerable<TItem> items, Func<TItem, TResult> selector) =>
        items.Any()
            ? items.Max(selector)
            : default;

    /// <summary>
    ///     Builds the joined string.
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    /// <param name="items">The items.</param>
    /// <returns>The joined string.</returns>
    public static string ToJoinedString<TItem>(this IEnumerable<TItem> items) =>
        string.Join(", ", [.. items.Select(i => i.ToString())]);

    /// <summary>
    ///     Converts <see cref="IEnumerable{T}" /> to the <see cref="ObservableCollection{T}" />.
    /// </summary>
    /// <typeparam name="T">Type of source</typeparam>
    /// <param name="source">The source.</param>
    /// <returns>
    ///     <see cref="ObservableCollection{T}" />
    /// </returns>
    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source) => new(source);

    /// <summary>
    ///     Finds the index of the first occurrence of an item in an enumerable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">The enumerable to search.</param>
    /// <param name="item">The item to find.</param>
    /// <returns>
    ///     The index of the first matching item, or -1 if the item was not found.
    /// </returns>
    public static int IndexOf<T>(this IEnumerable<T> items, T item) =>
        items.IndexWhere(i => ObjectExtensions.IsEqual(ref item, i));

    /// <summary>
    ///     Gets index of first element where condition is met.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="predicate">The predicate.</param>
    /// <returns>
    ///     IEnumerable{System.Int32}
    /// </returns>
    public static int IndexWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var index = 0;

        foreach (T element in source)
        {
            if (predicate(element))
                return index;

            index++;
        }

        return -1;
    }

    /// <summary>
    ///     Gets indexes of all elements where condition is met.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="predicate">The predicate.</param>
    /// <returns>
    ///     IEnumerable{System.Int32}
    /// </returns>
    public static IEnumerable<int> IndexesWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var index = 0;

        foreach (T element in source)
        {
            if (predicate(element))
                yield return index;

            index++;
        }
    }

    /// <summary>
    ///     Gets the type of the item.
    /// </summary>
    /// <param name="enumerable">The enumerable.</param>
    /// <returns></returns>
    public static Type GetItemType(this IEnumerable enumerable) => enumerable.GetType().GetElementType();

    /// <summary>
    ///     Checks if two sequences contain the same elements without checking their order
    /// </summary>
    /// <param name="first">The first sequence.</param>
    /// <param name="second">The second sequence.</param>
    /// <returns><c>true</c> if sequences contain the same elements; otherwise, <c>false</c>.</returns>
    public static bool UnorderedSequenceEqual(this IEnumerable first, IEnumerable second) =>
        first.Cast<object>().OrderBy(t => t).SequenceEqual(second.Cast<object>().OrderBy(t => t));

    /// <summary>
    ///     Checks if two sequences contain the same elements without checking their order
    /// </summary>
    /// <param name="first">The first sequence.</param>
    /// <param name="second">The second sequence.</param>
    /// <returns><c>true</c> if sequences contain the same elements; otherwise, <c>false</c>.</returns>
    public static bool UnorderedSequenceEqual<T>(this IEnumerable<T> first, IEnumerable<T> second) =>
        first.OrderBy(t => t).SequenceEqual(second.OrderBy(t => t));

    /// <summary>
    ///     To the collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <returns>
    ///     Collection{T}
    /// </returns>
    public static Collection<T> ToCollection<T>(this IEnumerable<T> source) => new([.. source]);

    public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int n) =>
        source.Skip(Math.Max(0, source.Count() - n));

    /// <summary>
    ///     Multiplies the given IEnumerables by given one (builds cartesian result).
    /// </summary>
    /// <typeparam name="T">Type of the item</typeparam>
    /// <param name="origin">The origin.</param>
    /// <param name="multiplier">The multiplier.</param>
    /// <returns>IEnumerables multiplied by the give one.</returns>
    public static IList<IEnumerable<T>> MulitplyBy<T>(this IEnumerable<IEnumerable<T>> origin,
                                                      IEnumerable<T> multiplier)
    {
        var defreedList = origin.ToList();

        return
        [
            .. defreedList.Count != 0
                ? multiplier.SelectMany(item => defreedList.Select(list => new List<T>(list)
                {
                    item
                }))
                : multiplier.Select(item => new List<T>
                {
                    item
                })
        ];
    }

    public static int CountEqualItems<T>(this IEnumerable<T> sourceA, IEnumerable<T> sourceB) where T : IEquatable<T>
    {
        var listA = sourceA.ToList();
        var listB = sourceB.ToList();

        int listACount = listA.Count;
        int listBCount = listB.Count;

        IEnumerable<T> shorter = listACount <= listBCount
            ? listA
            : listB;

        IEnumerable<T> longer = listACount <= listBCount
            ? listB
            : listA;

        int shorterCount = shorter.Count();
        int longerCount = longer.Count();
        var arrayB = new BitArray(shorterCount);
        var count = 0;

        for (var i = 0; i < shorterCount; i++)
        {
            T tA = shorter.ElementAt(i);

            for (var j = 0; j < longerCount; j++)
            {
                if (!arrayB[i])
                {
                    T tB = longer.ElementAt(j);

                    if (tA.Equals(tB))
                    {
                        count++;
                        arrayB[i] = true;
                    }
                }
            }
        }

        return count;
    }

    public static IEnumerable<T> GetAllItemsChildren<T>(this IEnumerable<T> items,
                                                        Func<T, IEnumerable<T>> getChildrenFunc) =>
        items?.SelectMany(item => item.Yield().Concat(GetAllItemChildren(item, getChildrenFunc)));

    public static IEnumerable<T> GetAllItemChildren<T>(this T item, Func<T, IEnumerable<T>> getChildrenFunc)
    {
        IEnumerable<T> children = getChildrenFunc(item);

        return children.IsNotNullOrEmptyEnumerable()
            ? children.Concat(children.SelectMany(x => GetAllItemChildren(x, getChildrenFunc)))
            : [];
    }
}