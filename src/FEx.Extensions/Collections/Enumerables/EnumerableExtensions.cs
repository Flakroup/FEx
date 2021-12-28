using JetBrains.Annotations;
using System.Collections;
using System.Collections.ObjectModel;

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
    public static bool IsNullOrEmptyEnumerable<T>(this IEnumerable<T> source)
    {
        return source?.Any() != true;
    }

    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyEnumerable<T>(this IEnumerable<T> source)
    {
        return source?.Any() == true;
    }

    /// <summary>
    ///     Appends a sequence of items to an existing list
    /// </summary>
    /// <typeparam name="T">The type of the items in the list.</typeparam>
    /// <param name="source">The list to modify.</param>
    /// <param name="items">The sequence of items to add to the list.</param>
    /// <returns></returns>
    public static void AddRange<T>(ref IEnumerable<T> source, IEnumerable<T> items)
    {
        source = source.Concat(items);
    }

    /// <summary>
    ///     Aggregates a list of strings.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>A comma separated string with values if any; Otherwise a empty string.</returns>
    public static string AggregateSafe(this IEnumerable<string> source)
    {
        IEnumerable<string> enumerable = source as string[] ?? source.ToArray();
        var result = string.Empty;
        if (enumerable.Any())
        {
            result = string.Join(", ", enumerable);
        }

        return result;
    }

    /// <summary>
    ///     Execute a action for each item in the list.
    /// </summary>
    /// <typeparam name="T">Sequence element type.</typeparam>
    /// <param name="source">The list itself.</param>
    /// <param name="action">Action to take on each item</param>
    /// <returns>The list itself.</returns>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (T item in source)
        {
            action(item);
        }
    }

    /// <summary>
    ///     Searches for an element that matches the conditions defined by the specified predicate, and returns the first
    ///     occurrence.
    /// </summary>
    /// <typeparam name="T">Sequence element type.</typeparam>
    /// <param name="source">The list itself.</param>
    /// <param name="predicate">Condition of the element to search for.</param>
    /// <returns>If found, an element of type T; otherwise default(T).</returns>
    public static T Find<T>(this IEnumerable<T> source, Func<T, bool> predicate = null)
    {
        bool Predicate(T i)
        {
            return predicate?.Invoke(i) ?? true;
        }

        switch (source)
        {
            case T[] array:
                return Array.Find(array, Predicate);
            case List<T> list:
                return list.Find(Predicate);
            default:
                return source.FirstOrDefault(Predicate);
        }
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
        {
            multipliedItems.AddRange(items);
        }

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
        IList<T> partialList = new List<T>();
        var counter = 0;

        foreach (T item in items)
        {
            if (counter == 0 || counter % maxNumberOfItems == 0)
            {
                partialList = new List<T>();
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
    public static TResult MaxOrDefault<TItem, TResult>(this IEnumerable<TItem> items, Func<TItem, TResult> selector)
    {
        return items.Any() ? items.Max(selector) : default;
    }

    /// <summary>
    ///     Builds the joined string.
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    /// <param name="items">The items.</param>
    /// <returns>The joined string.</returns>
    public static string ToJoinedString<TItem>(this IEnumerable<TItem> items)
    {
        return string.Join(", ", items.Select(i => i.ToString()).ToArray());
    }

    /// <summary>
    ///     Converts <see cref="IEnumerable{T}" /> to the <see cref="ObservableCollection{T}" />.
    /// </summary>
    /// <typeparam name="T">Type of source</typeparam>
    /// <param name="source">The source.</param>
    /// <returns>
    ///     <see cref="ObservableCollection{T}" />
    /// </returns>
    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
    {
        return new ObservableCollection<T>(source);
    }

    /// <summary>
    ///     Finds the index of the first occurrence of an item in an enumerable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">The enumerable to search.</param>
    /// <param name="item">The item to find.</param>
    /// <returns>
    ///     The index of the first matching item, or -1 if the item was not found.
    /// </returns>
    public static int IndexOf<T>(this IEnumerable<T> items, T item)
    {
        return items.IndexWhere(i => ObjectExtensions.IsEqual(ref item, i));
    }

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
            {
                return index;
            }

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
            {
                yield return index;
            }

            index++;
        }
    }

    /// <summary>
    ///     Gets the type of the item.
    /// </summary>
    /// <param name="enumerable">The enumerable.</param>
    /// <returns></returns>
    public static Type GetItemType(this IEnumerable enumerable)
    {
        return enumerable.GetType().GetElementType();
    }

    /// <summary>
    ///     Checks if two sequences contain the same elements without checking their order
    /// </summary>
    /// <param name="first">The first sequence.</param>
    /// <param name="second">The second sequence.</param>
    /// <returns><c>true</c> if sequences contain the same elements; otherwise, <c>false</c>.</returns>
    public static bool UnorderedSequenceEqual(this IEnumerable first, IEnumerable second)
    {
        return first.Cast<object>().OrderBy(t => t).SequenceEqual(second.Cast<object>().OrderBy(t => t));
    }

    /// <summary>
    ///     To the collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <returns>
    ///     Collection{T}
    /// </returns>
    public static Collection<T> ToCollection<T>(this IEnumerable<T> source)
    {
        return new(source.ToList());
    }

    public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int n)
    {
        return source.Skip(Math.Max(0, source.Count() - n));
    }

    public static IEnumerable<T> DistinctBy<T>(this IEnumerable<T> enumerable, Func<T, object> propertySelector)
    {
        return enumerable
            .GroupBy(propertySelector)
            .Select(g => g.First());
    }

    /// <summary>
    ///     Multiplies the given IEnumerables by given one (builds cartesian result).
    /// </summary>
    /// <typeparam name="T">Type of the item</typeparam>
    /// <param name="origin">The origin.</param>
    /// <param name="multiplier">The multiplier.</param>
    /// <returns>IEnumerables multiplied by the give one.</returns>
    public static IList<IEnumerable<T>> MulitplyBy<T>(this IEnumerable<IEnumerable<T>> origin, IEnumerable<T> multiplier)
    {
        IList<IEnumerable<T>> multipliedLists = new List<IEnumerable<T>>();
        if (origin.Any())
        {
            foreach (T item in multiplier)
            {
                foreach (IEnumerable<T> list in origin)
                {
                    var multipliedList = new List<T>(list) {item};
                    multipliedLists.Add(multipliedList);
                }
            }
        }
        else
        {
            foreach (T item in multiplier)
            {
                var multipliedList = new List<T> {item};
                multipliedLists.Add(multipliedList);
            }
        }

        return multipliedLists;
    }

    public static int CountEqualItems<T>(this IEnumerable<T> listA, IEnumerable<T> listB)
        where T : IEquatable<T>
    {
        int listACount = listA.Count();
        int listBCount = listB.Count();
        IEnumerable<T> shorter = listACount <= listBCount ? listA : listB;
        IEnumerable<T> longer = listACount <= listBCount ? listB : listA;

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

    public static IEnumerable<T> GetAllItemsChildren<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> getChildrenFunc)
    {
        return items?.SelectMany(item => item.Yield().Concat(GetAllItemChildren(item, getChildrenFunc)));
    }

    public static IEnumerable<T> GetAllItemChildren<T>(this T item, Func<T, IEnumerable<T>> getChildrenFunc)
    {
        IEnumerable<T> children = getChildrenFunc(item);

        return children.IsNotNullOrEmptyEnumerable() ? children.Concat(children.SelectMany(x => GetAllItemChildren(x, getChildrenFunc))) : Enumerable.Empty<T>();
    }

    public static IOrderedEnumerable<string> OrderAlphanumBy(this IEnumerable<string> source)
    {
        return source.OrderAlphanumBy(x => x);
    }

    public static IOrderedEnumerable<TSource> OrderAlphanumBy<TSource>(this IEnumerable<TSource> source, Func<TSource, string> keySelector)
    {
        return source.OrderBy(keySelector, AlphanumComparatorFast.Instance);
    }

    public static IOrderedEnumerable<string> OrderAlphanumByDescending(this IEnumerable<string> source)
    {
        return source.OrderAlphanumByDescending(x => x);
    }

    public static IOrderedEnumerable<TSource> OrderAlphanumByDescending<TSource>(this IEnumerable<TSource> source, Func<TSource, string> keySelector)
    {
        return source.OrderByDescending(keySelector, AlphanumComparatorFast.Instance);
    }
}