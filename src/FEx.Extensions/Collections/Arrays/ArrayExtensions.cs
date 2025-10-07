using System;

namespace FEx.Extensions.Collections.Arrays;

public static class ArrayExtensions
{
    /// <summary>
    /// Check if the index is within the array
    /// </summary>
    /// <param name="source"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static bool WithinIndex(this Array source, int index) =>
        source is not null && index >= 0 && index < source.Length;

    /// <summary>
    /// Indexes the of.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static int IndexOf(this Array source, object value)
    {
        for (var i = 0; i < source.Length; i++)
        {
            if (source.GetValue(i) == value)
                return i;
        }

        return -1;
    }
}