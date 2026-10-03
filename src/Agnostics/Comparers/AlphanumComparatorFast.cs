using System;
using System.Collections.Generic;

namespace FEx.Agnostics.Comparers;

/// <summary>Compares strings in natural order, treating runs of digits as numbers so that "file2" sorts before "file10".</summary>
public sealed class AlphanumComparatorFast : IComparer<string>, IEqualityComparer<string>
{
    /// <summary>Compares two strings in natural order using the current culture.</summary>
    /// <param name="s1">The first string.</param>
    /// <param name="s2">The second string.</param>
    /// <returns>A negative, zero or positive value when <paramref name="s1"/> sorts before, equal to or after <paramref name="s2"/>; zero if either is <see langword="null"/>.</returns>
    public int Compare(string? s1, string? s2) => Compare(s1, s2, StringComparison.CurrentCulture);

    /// <summary>Determines whether two strings compare as equal in natural order.</summary>
    /// <param name="x">The first string.</param>
    /// <param name="y">The second string.</param>
    /// <returns><see langword="true"/> if the natural-order comparison yields zero.</returns>
    public bool Equals(string? x, string? y) => Compare(x, y) == 0;

    /// <summary>Returns the standard hash code of the string.</summary>
    /// <param name="obj">The string to hash.</param>
    /// <returns>The hash code of <paramref name="obj"/>.</returns>
    public int GetHashCode(string obj) => obj.GetHashCode();

    /// <summary>Compares two strings in natural order, comparing digit runs numerically and other runs with the given comparison type.</summary>
    /// <param name="s1">The first string.</param>
    /// <param name="s2">The second string.</param>
    /// <param name="comparisonType">How the non-numeric chunks are compared.</param>
    /// <returns>A negative, zero or positive value when <paramref name="s1"/> sorts before, equal to or after <paramref name="s2"/>; zero if either is <see langword="null"/>.</returns>
    /// <exception cref="OverflowException">A digit run exceeds <see cref="long.MaxValue"/>.</exception>
    public static int Compare(string? s1, string? s2, StringComparison comparisonType)
    {
        if (s1 is null
            || s2 is null)
            return 0; // or consider throwing an ArgumentNullException

        int marker1 = 0, marker2 = 0;

        while (marker1 < s1.Length
               && marker2 < s2.Length)
        {
            // Build up two strings to compare either numerically or alphabetically
            var str1 = ExtractChunk(s1, ref marker1);
            var str2 = ExtractChunk(s2, ref marker2);

            int result;

            if (char.IsDigit(str1[0])
                && char.IsDigit(str2[0]))
            {
                // Use long instead of int to handle larger numbers (timestamps, hashes, etc.)
                // Will throw OverflowException if number exceeds long.MaxValue - fix with BigInteger if needed
                var numeric1 = long.Parse(str1);
                var numeric2 = long.Parse(str2);
                result = numeric1.CompareTo(numeric2);
            }
            else
            {
                result = string.Compare(str1, str2, comparisonType);
            }

            if (result != 0)
                return result;
        }

        return s1.Length - s2.Length;
    }

    private static string ExtractChunk(string str, ref int marker)
    {
        var originalMarker = marker;
        var isDigit = char.IsDigit(str[marker]);

        while (marker < str.Length
               && char.IsDigit(str[marker]) == isDigit)
            marker++;

        return str.Substring(originalMarker, marker - originalMarker);
    }
}