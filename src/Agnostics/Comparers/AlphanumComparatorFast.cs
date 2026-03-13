using System;
using System.Collections.Generic;

namespace FEx.Agnostics.Comparers;

public sealed class AlphanumComparatorFast : IComparer<string>, IEqualityComparer<string>
{
    public int Compare(string s1, string s2) => Compare(s1, s2, StringComparison.CurrentCulture);

    public bool Equals(string x, string y) => Compare(x, y) == 0;

    public int GetHashCode(string obj) => obj.GetHashCode();

    public static int Compare(string s1, string s2, StringComparison comparisonType)
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