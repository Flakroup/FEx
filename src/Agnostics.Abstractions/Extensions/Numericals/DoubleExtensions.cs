using System;
using System.Globalization;
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

    // The probe is a double whose shortest round-trip form needs 17 digits; "G15" (the .NET Framework default) renders it as "0.3".
    private const double RoundTripProbe = 0.1 + 0.2;

    // null selects the default format. Detected at run time, not per target framework: a netstandard2.0 build runs on both
    // .NET Framework (default "G15", lossy) and .NET Core 3.0+ (default is the shortest round-trippable string).
    private static readonly string? RoundTripFormat = DefaultFormatRoundTrips() ? null : "G17";

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
    /// <returns>The parsed number; <c>-0</c> parses to negative zero on every runtime.</returns>
    /// <exception cref="FormatException"><paramref name="value" /> is not a number.</exception>
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

        if (!double.TryParse(value, out var l))
            throw new FormatException("Cannot unmarshal type double");

        // .NET Framework parses "-0" as +0; later runtimes keep the sign, so restore it there.
        return l == 0 && IsNegativeText(value) ? -0.0 : l;
    }

    /// <summary>
    /// Formats a double with the invariant culture so that parsing the result returns exactly the same double on every runtime:
    /// the default (shortest round-trippable) format where the runtime has one, "G17" where the default is "G15".
    /// </summary>
    /// <param name="value">The number to format. NaN and the infinities are written as <c>NaN</c>, <c>Infinity</c> and <c>-Infinity</c>, negative zero as <c>-0</c>.</param>
    /// <returns>The invariant-culture text of <paramref name="value" />.</returns>
    public static string ToRoundTripString(this double value) =>
        value == 0 && BitConverter.DoubleToInt64Bits(value) < 0 // .NET Framework formats -0.0 as "0"
            ? "-0"
            : value.ToString(RoundTripFormat, CultureInfo.InvariantCulture);

    private static bool IsNegativeText(string value)
    {
        var text = value.TrimStart();

        return text.StartsWith("-", StringComparison.Ordinal)
               || text.StartsWith(CultureInfo.CurrentCulture.NumberFormat.NegativeSign, StringComparison.Ordinal);
    }

    private static bool DefaultFormatRoundTrips() =>
        double.Parse(RoundTripProbe.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture) == RoundTripProbe;

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