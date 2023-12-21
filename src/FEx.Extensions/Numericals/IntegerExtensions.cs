using System;

namespace FEx.Extensions.Numericals;

public static class IntegerExtensions
{
    /// <summary>
    ///     Adds the value to source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="value">The value.</param>
    /// <returns>A int.</returns>
    public static int Add(this int source, int value) => source + value;

    /// <summary>
    ///     Indicates that specified value is equal to 0
    /// </summary>
    /// <param name="value">Integer to test.</param>
    /// <returns>True, if the current values is 0; otherwise, false</returns>
    public static bool IsZero(this int value) => value == 0;

    /// <summary>
    ///     Indicates that specified value is > than 0
    /// </summary>
    /// <param name="value">Integer to test.</param>
    /// <returns>True, if the current values is > 0; otherwise, false</returns>
    public static bool IsNotZero(this int value) => value > 0;

    /// <summary>
    ///     Gets a value indicating if value is between or equal Minimum - Maximum values.
    /// </summary>
    /// <param name="value">The int.</param>
    /// <param name="min">Minimum value to test aginst.</param>
    /// <param name="max">Maximum value to test aginst.</param>
    /// <returns>True if in Minimum - Maximum range; otherwise False.</returns>
    public static bool InRange(this int value, int min, int max) => value >= min && value <= max;

    /// <summary>
    ///     Gets a value indicating if value is between (or equal) Minimum - Maximum values.
    /// </summary>
    /// <param name="value">The int value.</param>
    /// <param name="min">Minimum value to test aginst.</param>
    /// <param name="max">Maximum value to test aginst.</param>
    /// <param name="includeMinMaxValues">If set to <c>true</c> [include minimum maximum values].</param>
    /// <returns>True if in Minimum - Maximum range; otherwise False.</returns>
    public static bool InRange(this int value, int min, int max, bool includeMinMaxValues) =>
        includeMinMaxValues
            ? value.InRange(min, max)
            : value > min && value < max;

    /// <summary>
    ///     Indicates that specified value is less than zero.
    /// </summary>
    /// <param name="value">Int to test.</param>
    /// <returns>True, if the current value is less than zero; otherwise, false.</returns>
    public static bool IsLessThanZero(this int value) => value < 0;

    /// <summary>
    ///     Converts the integer to a string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as string it not null; Otherwise a empty string.</returns>
    public static string ToStringSafe(this int? value) => value.ToStringSafe(string.Empty);

    /// <summary>
    ///     Converts the integer to a string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The value as string it not null; Otherwise the default value.</returns>
    public static string ToStringSafe(this int? value, string defaultValue) => value?.ToString() ?? defaultValue;

    /// <summary>
    ///     Converts the integer to a byte.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as byte it not null; Otherwise zero.</returns>
    public static byte ToByteSafe(this int? value) =>
        value.HasValue
            ? (byte)value.Value
            : (byte)0;

    /// <summary>
    ///     Converts the integer to a byte.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The value as byte it not null; Otherwise default value.</returns>
    public static byte ToByteSafe(this int? value, byte defaultValue) =>
        value.HasValue
            ? (byte)value.Value
            : defaultValue;

    public static short ToShort(this int value) =>
        value is > short.MaxValue or < short.MinValue
            ? throw new ArgumentOutOfRangeException(nameof(value),
                "Provided argument value is outside of short type values range")
            : Convert.ToInt16(value);
}