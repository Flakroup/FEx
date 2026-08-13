using System.Collections.Generic;
using System.Globalization;

namespace FEx.Agnostics.Comparers;

public class NonSpaceIgnoringStringComparer : IEqualityComparer<string>
{
    public bool Equals(string? x, string? y) =>
        string.Compare(x, y, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace) == 0;

    public int GetHashCode(string obj) => obj.GetHashCode();
}