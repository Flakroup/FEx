using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FEx.Extensions.Collections.Lists;

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
                for (int i = list.Count - 1; i > -1; i--)
                {
                    if (predicate(list[i]))
                    {
                        list.RemoveAt(i);
                        anyItemHasMatched = true;
                    }
                }

                break;
            default:
                for (int i = source.Count - 1; i > -1; i--)
                {
                    T element = source.ElementAt(i);

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

            for (int i = index; i < index + count; i++)
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

        foreach (T f in sourceList)
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
        IList<T> shorter = listA.Count <= listB.Count
            ? listA
            : listB;

        IList<T> longer = listA.Count <= listB.Count
            ? listB
            : listA;

        int shorterCount = shorter.Count;
        int longerCount = longer.Count;
        var arrayB = new BitArray(shorterCount);
        var count = 0;

        for (var i = 0; i < shorterCount; i++)
        {
            T tA = shorter[i];

            for (var j = 0; j < longerCount; j++)
            {
                if (!arrayB[i])
                {
                    T tB = longer[j];

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

    public static void AddRangeToList<T, TColl>(this TColl source, IEnumerable<T> items) where TColl : IList<T>
    {
        if (source is List<T> list)
            list.AddRange(items);
        else
            source.AddRangeToCollection(items);
    }

    public static void Move<T>(this IList<T> source, int oldIndex, int newIndex)
    {
        T item = source[oldIndex];
        source.RemoveAt(oldIndex);
        source.Insert(newIndex, item);
    }
}