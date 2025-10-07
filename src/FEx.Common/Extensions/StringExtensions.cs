using JetBrains.Annotations;
using System;

namespace FEx.Common.Extensions;

public static class StringExtensions
{
    public static bool CompareOrdinalIgnoreCase(this string source, string value) =>
        string.Compare(source, value, StringComparison.OrdinalIgnoreCase) == 0;

    /// <summary>
    /// Indicates whether a string contains another string under <see cref="StringComparison.OrdinalIgnoreCase" />
    /// comparison.
    /// </summary>
    public static bool ContainsOrdinalIgnoreCase(this string str, string other) =>
#if NETSTANDARD
        str.IndexOf(other, StringComparison.OrdinalIgnoreCase) >= 0;
#else
        str.Contains(other, StringComparison.OrdinalIgnoreCase);
#endif

    /// <summary>
    /// Compare 2 strings, ignoring case.
    /// </summary>
    /// <param name="source">First value to compare with.</param>
    /// <param name="value">Second value to compare with.</param>
    /// <param name="comparisonType">Type of the comparison.</param>
    /// <returns>
    /// True if equal otherwise False.
    /// </returns>
    public static bool IsEqual(this string source,
                               string value,
                               StringComparison comparisonType = StringComparison.OrdinalIgnoreCase) =>
        string.Equals(source, value, comparisonType);

    /// <summary>
    /// Determines whether string is not equal to the specified value.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="value">The value.</param>
    /// <param name="comparisonType">Type of the comparison.</param>
    /// <returns>
    /// <c>true</c> if it is not equal to the specified value; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNotEqual(this string source,
                                  string value,
                                  StringComparison comparisonType = StringComparison.OrdinalIgnoreCase) =>
        !source.IsEqual(value, comparisonType);

    /// <summary>
    /// Gets a value indicating if the string is Null or Empty.
    /// </summary>
    /// <param name="value">string to test.</param>
    /// <returns>True if string is Null or Empty otherwise False.</returns>
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmptyString(this string value) => value is null || string.IsNullOrEmpty(value);

    /// <summary>
    /// Gets a value indicating if the string is NOT Null or Empty.
    /// </summary>
    /// <param name="value">string to test.</param>
    /// <returns>True if string is Null or Empty otherwise False.</returns>
    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyString(this string value) => value is not null && !string.IsNullOrEmpty(value);
}