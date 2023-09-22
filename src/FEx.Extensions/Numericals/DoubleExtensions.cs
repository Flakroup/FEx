using System;
using System.Threading;

namespace FEx.Extensions.Numericals;

public static class DoubleExtensions
{
    private const double D1 = 0.1;
    private const double D2 = 0.01;
    private const double D3 = 0.001;
    private const double D4 = 0.0001;
    private const double D5 = 0.00001;
    private const double D6 = 0.000001;
    private const double D7 = 0.0000001;

    public static bool PreciseEquals(this double left, double right, int floatDigits = 7)
    {
        if (floatDigits is < 1 or > 7)
            throw new ArgumentOutOfRangeException(nameof(floatDigits), floatDigits,
                "Only values between 1 and 7 are supported");

        double floatComparison = GetFloatComparison(floatDigits);
        return Math.Abs(left - right) < floatComparison;
    }

    public static double FromString(this string value)
    {
        string numberDecimalSeparator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        if (value.Contains(".")
            && "." != numberDecimalSeparator)
            value = value.Replace(".", numberDecimalSeparator);
        else if (value.Contains(",")
                 && "," != numberDecimalSeparator)
            value = value.Replace(",", numberDecimalSeparator);

        return double.TryParse(value, out double l)
            ? l
            : throw new Exception("Cannot unmarshal type double");
    }

    private static double GetFloatComparison(int floatDigits)
    {
        return floatDigits switch
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
}