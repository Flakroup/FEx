using System;
using System.Threading;

namespace FEx.Agnostics.Abstractions.Extensions.Numericals;

/// <summary>Extensions for comparing and parsing doubles.</summary>
public static class DoubleExtensions
{
    private const double D1 = 0.1;
    private const double D2 = 0.01;
    private const double D3 = 0.001;
    private const double D4 = 0.0001;
    private const double D5 = 0.00001;
    private const double D6 = 0.000001;
    private const double D7 = 0.0000001;

    /// <summary>Determines whether two doubles differ by less than a tolerance of ten to the power of minus the given digits.</summary>
    /// <param name="left">The first number.</param>
    /// <param name="right">The second number.</param>
    /// <param name="floatDigits">The number of fractional digits that must match, from 1 to 7.</param>
    /// <returns><c>true</c> if the numbers are equal within the tolerance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="floatDigits" /> is not between 1 and 7.</exception>
    public static bool PreciseEquals(this double left, double right, int floatDigits = 7)
    {
        if (floatDigits is < 1 or > 7)
            throw new ArgumentOutOfRangeException(nameof(floatDigits),
                floatDigits,
                "Only values between 1 and 7 are supported");

        var floatComparison = GetFloatComparison(floatDigits);

        return Math.Abs(left - right) < floatComparison;
    }

    /// <summary>Parses a string to a double, accepting either '.' or ',' as the decimal separator regardless of the current culture.</summary>
    /// <param name="value">The text to parse.</param>
    /// <returns>The parsed number.</returns>
    public static double ToDouble(this string value)
    {
        var numberDecimalSeparator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        if (
#if NETSTANDARD
            value.Contains(".")
#else
            value.Contains('.')
#endif
            && "." != numberDecimalSeparator)
            value = value.Replace(".", numberDecimalSeparator);
        else if (
#if NETSTANDARD
            value.Contains(",")
#else
            value.Contains(',')
#endif
            && "," != numberDecimalSeparator)
            value = value.Replace(",", numberDecimalSeparator);

        return double.TryParse(value, out var l)
            ? l
            : throw new("Cannot unmarshal type double");
    }

    private static double GetFloatComparison(int floatDigits) =>
        floatDigits switch
        {
            1 => D1,
            2 => D2,
            3 => D3,
            4 => D4,
            5 => D5,
            6 => D6,
            7 => D7,
            _ => 0
        };
}