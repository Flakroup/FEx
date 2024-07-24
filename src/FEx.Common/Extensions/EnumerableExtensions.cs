using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Common.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    ///     Searches for an element that matches the conditions defined by the specified predicate, and returns the first
    ///     occurrence.
    /// </summary>
    /// <typeparam name="T">Sequence element type.</typeparam>
    /// <param name="source">The list itself.</param>
    /// <param name="predicate">Condition of the element to search for.</param>
    /// <returns>If found, an element of type T; otherwise default(T).</returns>
    public static T FindInEnumerable<T>(this IEnumerable<T> source, Func<T, bool> predicate = null)
    {
        return source switch
        {
            T[] array => Array.Find(array, Predicate),
            List<T> list => list.Find(Predicate),
            _ => source.FirstOrDefault(Predicate)
        };

        bool Predicate(T i) => predicate?.Invoke(i) ?? true;
    }
}