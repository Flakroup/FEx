using FEx.Core.Collections.Concurrent;
using System;
using System.Collections.Generic;

namespace FEx.Core.Extensions;

public static class EnumerableExtensions
{
    public static ConcurrentObservableList<T> ToConcurrentObservableList<T>(this IEnumerable<T> source) => new(source);

    public static ConcurrentSortableObservableList<T> ToConcurrentSortableObservableList<T>(this IEnumerable<T> source)
        where T : IComparable<T> =>
        new(source);
}