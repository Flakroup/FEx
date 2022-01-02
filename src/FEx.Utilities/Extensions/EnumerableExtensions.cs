using FEx.Utilities.Comparers;

namespace FEx.Utilities.Extensions;

/// <summary>
///     IEnumerable interface extensions.
/// </summary>
public static class EnumerableExtensions
{
    public static IOrderedEnumerable<string> OrderAlphanumBy(this IEnumerable<string> source)
    {
        return source.OrderAlphanumBy(x => x);
    }

    public static IOrderedEnumerable<string> OrderAlphanumByDescending(this IEnumerable<string> source)
    {
        return source.OrderAlphanumByDescending(x => x);
    }

    public static IOrderedEnumerable<TSource> OrderAlphanumBy<TSource>(this IEnumerable<TSource> source, Func<TSource, string> keySelector)
    {
        return source.OrderBy(keySelector, AlphanumComparatorFast.Instance);
    }

    public static IOrderedEnumerable<TSource> OrderAlphanumByDescending<TSource>(this IEnumerable<TSource> source, Func<TSource, string> keySelector)
    {
        return source.OrderByDescending(keySelector, AlphanumComparatorFast.Instance);
    }
}