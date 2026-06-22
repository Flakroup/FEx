using FEx.Agnostics.Abstractions.Interfaces;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FEx.Agnostics.Abstractions.Extensions.Collections.Lists;

/// <summary>
/// Extensions for the IList interface.
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// Gets a value indicating if the collection contains data.
    /// </summary>
    /// <typeparam name="T">Sequence element type.</typeparam>
    /// <param name="source">The list itself.</param>
    /// <returns>True if collection has rows otherwise False.</returns>
    public static bool IsNotEmpty<T>(this IList<T> source) => (source?.Count ?? 0) > 0;

    /// <summary>
    /// Gets a value indicating if the collection contains data.
    /// </summary>
    /// <typeparam name="T">Sequence element type.</typeparam>
    /// <param name="source">The list itself.</param>
    /// <returns>True if collection has rows otherwise False.</returns>
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmptyList<T>(this IList<T> source) => source is null || source.Count == 0;

    /// <summary>
    /// Determines whether [is not null neither is empty].
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <returns>
    /// <c>true</c> if [is not null neither is empty] [the specified source]; otherwise, <c>false</c>.
    /// </returns>
    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyList<T>(this IList<T> source) => source?.Count > 0;

    /// <summary>
    /// Converts to a readonly collection.
    /// </summary>
    /// <typeparam name="T">The type of source.</typeparam>
    /// <param name="source">The source.</param>
    /// <returns>A ReadOnlyCollection{T} instance.</returns>
    public static ReadOnlyCollection<T> ToReadOnly<T>(this IList<T> source) => new(source);

    public static bool RemoveFromListWhere<T>(this ICollection<T> source, Func<T, bool> predicate)
    {
        var anyItemHasMatched = false;

        switch (source)
        {
            case HashSet<T> hashSet:
                anyItemHasMatched = hashSet.RemoveWhere(Predicate) != 0;

                break;
            case List<T> list:
                anyItemHasMatched = list.RemoveAll(Predicate) != 0;

                break;
            case IList<T> list:
                for (var i = list.Count - 1; i > -1; i--)
                {
                    if (predicate(list[i]))
                    {
                        list.RemoveAt(i);
                        anyItemHasMatched = true;
                    }
                }

                break;
            default:
                for (var i = source.Count - 1; i > -1; i--)
                {
                    var element = source.ElementAt(i);

                    if (predicate(element))
                    {
                        source.Remove(element);
                        anyItemHasMatched = true;
                    }
                }

                break;
        }

        return anyItemHasMatched;

        bool Predicate(T i) => predicate(i);
    }

    public static List<T> GetRange<T>(this IList<T> sourceList, int index, int count)
    {
        if (index > 0
            && count > 0
            && sourceList.IsNotNullOrEmptyList()
            && sourceList.Count - index > count)
        {
            var list = new List<T>(count);

            for (var i = index; i < index + count; i++)
                list.Add(sourceList[i]);

            return list;
        }

        return null;
    }

    public static IList<IList<T>> SplitList<T>(this IList<T> sourceList, int chunkSize)
    {
        var list = new List<IList<T>>();
        var sourceListCast = sourceList.ToList();

        for (var i = 0; i < sourceListCast.Count; i += chunkSize)
            list.Add(sourceListCast.GetRange(i, Math.Min(chunkSize, sourceList.Count - i)));

        return list;
    }

    public static void SyncWithItem<T>(this IList<T> sourceList, T item, Action<T, T> syncAction = null)
        where T : IEquatable<T>
    {
        var synced = false;

        foreach (var f in sourceList)
        {
            if (f.Equals(item))
            {
                syncAction?.Invoke(f, item);
                synced = true;
            }
        }

        if (!synced)
            sourceList.Add(item);
    }

    public static int CountEqualItems<T>(this IList<T> listA, IList<T> listB) where T : IEquatable<T>
    {
        var shorter = listA.Count <= listB.Count
            ? listA
            : listB;

        var longer = listA.Count <= listB.Count
            ? listB
            : listA;

        var shorterCount = shorter.Count;
        var longerCount = longer.Count;
        var arrayB = new BitArray(longerCount);
        var count = 0;

        for (var i = 0; i < shorterCount; i++)
        {
            var tA = shorter[i];

            for (var j = 0; j < longerCount; j++)
            {
                if (!arrayB[j])
                {
                    var tB = longer[j];

                    if (tA.Equals(tB))
                    {
                        count++;
                        arrayB[j] = true;

                        break;
                    }
                }
            }
        }

        return count;
    }

    public static void AddRangeToList<T, TColl>(this TColl source, IEnumerable<T> items) where TColl : IList<T>
    {
        if (source is List<T> list)
            list.AddRange(items);
        else
            source.AddRangeToCollection(items);
    }

    public static void Move<T>(this IList<T> source, int oldIndex, int newIndex)
    {
        var item = source[oldIndex];
        source.RemoveAt(oldIndex);
        source.Insert(newIndex, item);
    }

    /// <summary>
    /// Synchronizes two lists in one way mode.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sourceList">The source list.</param>
    /// <param name="syncedList">The list which state will be reflected in source.</param>
    /// <param name="equalityComparator">The equality comparator - must match unique objects.</param>
    /// <param name="syncAction">Action to be invoked on equal objects.</param>
    public static bool SyncWith<T>(this IList<T> sourceList,
                                   IList<T> syncedList,
                                   Func<T, T, bool> equalityComparator,
                                   Action<T, T> syncAction = null)
    {
        if (sourceList is IConcurrentList<T> t)
            return t.Combo(items => SyncWithCore(items, syncedList, equalityComparator, syncAction));

        return SyncWithCore(sourceList, syncedList, equalityComparator, syncAction);
    }

    // Non-concurrent core: must NOT re-check IConcurrentList. The concurrent overload above runs this inside
    // ConcurrentList.Combo (which passes 'this' to the callback); calling the public SyncWith there would re-enter
    // the concurrent branch and recurse forever (StackOverflowException).
    private static bool SyncWithCore<T>(IList<T> sourceList,
                                        IList<T> syncedList,
                                        Func<T, T, bool> equalityComparator,
                                        Action<T, T> syncAction)
    {
        var hasChanged = sourceList.RemoveFromListWhere(x => syncedList.All(y => !equalityComparator(x, y)));

        if (syncAction is not null)
            for (var i = 0; i < sourceList.Count; i++)
            {
                var f = sourceList[i];
                var match = syncedList.IndexesWhere(x => equalityComparator(f, x)).ToArray();

                if (match.Length == 1)
                {
                    syncAction(f, syncedList[match[0]]);

                    if (!ReferenceEquals(f, sourceList[i]))
                        hasChanged = true;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"{nameof(equalityComparator)} function doesn't provide unique objects.");
                }
            }

        var c = sourceList.Count;
        sourceList.AddRangeToList(syncedList.Where(x => sourceList.All(y => !equalityComparator(x, y))));

        if (c != sourceList.Count)
            hasChanged = true;

        return hasChanged;
    }

    public static bool SyncWith<T>(this IList<T> sourceList, IList<T> syncedList, Action<T, T> syncAction = null)
        where T : IEquatable<T>
    {
        if (sourceList is IConcurrentList<T> t)
            return t.Combo(items => SyncWithCore(items, syncedList, syncAction));

        return SyncWithCore(sourceList, syncedList, syncAction);
    }

    // Non-concurrent core: must NOT re-check IConcurrentList (see the equalityComparator overload above for why).
    private static bool SyncWithCore<T>(IList<T> sourceList, IList<T> syncedList, Action<T, T> syncAction)
        where T : IEquatable<T>
    {
        var hasChanged = sourceList.RemoveFromListWhere(x => syncedList.All(y => !x.Equals(y)));

        if (syncAction is not null)
            for (var i = 0; i < sourceList.Count; i++)
            {
                var f = sourceList[i];
                var match = syncedList.IndexesWhere(f.Equals).ToArray();

                if (match.Length == 1)
                {
                    syncAction(f, syncedList[match[0]]);

                    if (!ReferenceEquals(f, sourceList[i]))
                        hasChanged = true;
                }
                else
                {
                    throw new InvalidOperationException($"{nameof(Equals)} function doesn't provide unique objects.");
                }
            }

        var c = sourceList.Count;

        sourceList.AddRangeToList(syncedList.Where(x => sourceList.All(y => !x.Equals(y))));

        if (c != sourceList.Count)
            hasChanged = true;

        return hasChanged;
    }
}