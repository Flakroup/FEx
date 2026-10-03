using System.Collections.Generic;
using System.Globalization;

namespace FEx.Agnostics.Comparers;

/// <summary>Compares strings for equality using the current culture while ignoring nonspacing marks such as diacritics.</summary>
public class NonSpaceIgnoringStringComparer : IEqualityComparer<string>
{
    /// <summary>Determines whether two strings are equal when nonspacing marks are ignored.</summary>
    /// <param name="x">The first string.</param>
    /// <param name="y">The second string.</param>
    /// <returns><see langword="true"/> if the strings are equal ignoring nonspacing marks.</returns>
    public bool Equals(string? x, string? y) =>
        string.Compare(x, y, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace) == 0;

    /// <summary>Returns the standard hash code of the string.</summary>
    /// <param name="obj">The string to hash.</param>
    /// <returns>The hash code of <paramref name="obj"/>.</returns>
    public int GetHashCode(string obj) => obj.GetHashCode();
}