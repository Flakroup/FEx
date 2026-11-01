using System;

namespace FEx.Agnostics.Abstractions.Utilities;

/// <summary>Picks the Polish noun form that goes with a count, so a status line never says "1 plików".</summary>
public static class PolishPlural
{
    /// <summary>1 takes <paramref name="one"/>; a last digit of 2-4, except 12-14, takes <paramref name="few"/>; the rest takes <paramref name="many"/>.</summary>
    public static string Pick(int count, string one, string few, string many)
    {
        if (count == 1)
            return one;

        var lastTwo = Math.Abs(count) % 100;
        var last = lastTwo % 10;
        return last is >= 2 and <= 4 && lastTwo is < 12 or > 14 ? few : many;
    }
}
