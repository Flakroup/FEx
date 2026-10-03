using FEx.Agnostics.Abstractions.Utilities;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NotNullWhenAttribute = System.Diagnostics.CodeAnalysis.NotNullWhenAttribute;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>
/// String extensions class - comprehensive utilities for string manipulation.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Wild card position.
    /// </summary>
    public enum WildCardPosition
    {
        /// <summary>Wildcard at the start of the pattern.</summary>
        Start,
        /// <summary>Wildcard at the end of the pattern.</summary>
        End,
        /// <summary>Wildcard in the middle of the pattern.</summary>
        Middle,
        /// <summary>Wildcards at both the start and the end of the pattern.</summary>
        StartAndEnd
    }

    /// <summary>
    /// The standard wild card 'any value'.
    /// </summary>
    public const char StandardWildCardAnyValue = '*';

    /// <summary>
    /// The SQL wild card 'any value'.
    /// </summary>
    public const char SqlWildCardAnyValue = '%';

    /// <summary>
    /// The SQL wild card 'any value'.
    /// </summary>
    public const string SqlWildCardAnyValueEscaped = "[%]";

    /// <summary>
    /// The standard wild card 'any value'.
    /// </summary>
    public const char StandardWildCardOneCharacter = '?';

    /// <summary>
    /// The SQL wild card 'any value'.
    /// </summary>
    public const char SqlWildCardOneCharacter = '_';

    /// <summary>
    /// The SQL wild card 'any value'.
    /// </summary>
    public const string SqlWildCardOneCharacterEscaped = "[_]";

    private static readonly Regex _phoneNumberRegex = new("[^.0-9]", RegexOptions.Compiled);
    private static readonly int[] _doubledValues = [0, 2, 4, 6, 8, 1, 3, 5, 7, 9];

    /// <summary>Matches a run of word characters and apostrophes delimited by word boundaries.</summary>
    public static Regex WordRegex { get; } = new(@"\b[\w']+\b", RegexOptions.Compiled);
    /// <summary>Matches strings made only of ASCII letters.</summary>
    public static Regex LettersRegex { get; } = new("^[a-zA-Z]+$", RegexOptions.Compiled);
    /// <summary>Matches strings made only of ASCII letters and digits.</summary>
    public static Regex LettersAndNumbersRegex { get; } = new("^[a-zA-Z0-9]+$", RegexOptions.Compiled);
    /// <summary>Matches strings made only of ASCII letters, digits and underscores.</summary>
    public static Regex LettersNumbersAndUnderscoreRegex { get; } = new("^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    /// <summary>Capitalizes the first character and lower-cases the rest.</summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>The converted text, or null when the text is null, empty or white space.</returns>
    public static string? ToCamel(this string text) =>
        !string.IsNullOrWhiteSpace(text)
            ? $"{char.ToUpperInvariant(text[0])}{text.Substring(1).ToLowerInvariant()}"
            : null;

    /// <summary>Strips every character that is not a letter, apostrophe or white space.</summary>
    /// <param name="text">The text to clean.</param>
    /// <returns>The cleaned text, or null when the text is null, empty or white space.</returns>
    public static string? ToNiceString(this string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            return new([.. text.Where(static c => char.IsLetter(c) || c == '\'' || char.IsWhiteSpace(c))]);

        return null;
    }

    /// <summary>Removes every character other than digits and dots from a phone number, skipping the first two characters when it starts with '+'.</summary>
    /// <param name="phoneNumber">The phone number to format.</param>
    /// <returns>The formatted number, or null when the input is null, empty or white space.</returns>
    public static string? ToFormattedPhoneNumber(this string phoneNumber)
    {
        if (!string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber[0] == '+'
                ? _phoneNumberRegex.Replace(phoneNumber.Substring(2), string.Empty).Trim()
                : _phoneNumberRegex.Replace(phoneNumber, string.Empty).Trim();

        return null;
    }

    /// <summary>Appends each element of a collection to the builder without a separator.</summary>
    /// <param name="stringBuilder">The builder to append to.</param>
    /// <param name="collection">The values to append.</param>
    public static void AppendJoin(this StringBuilder stringBuilder, IEnumerable collection)
    {
        foreach (var value in collection)
            stringBuilder.Append(value);
    }

    /// <summary>
    /// Generate MD5 hash from the specified string
    /// </summary>
    /// <param name="value">Value to generate MD5 Hash</param>
    /// <returns>Calculated MD5 hash from the specified string</returns>
    public static string ComputeMd5Hash(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        using var md5 = MD5.Create();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(value));

#if NET9_0_OR_GREATER
        return Convert.ToHexStringLower(md5.ComputeHash(stream));
#else
        return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty).ToLower();
#endif
    }

    /// <summary>
    /// Returns a value indicating whether any of a set of specified substrings occurs within this string.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="toCheck">The set of strings to look for.</param>
    /// <param name="comparisonType">Type of the comparison.</param>
    /// <returns>
    /// true if any element from the <paramref name="toCheck">value</paramref> parameter occurs within this string or is
    /// empty; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="toCheck">value</paramref> is null.</exception>
    public static bool ContainsAny(this string value, IEnumerable<string> toCheck, StringComparison comparisonType)
    {
        if (value is null
            || toCheck.IsNullOrEmpty())
            return false;

#if NETSTANDARD2_0
        return toCheck.Any(x => value.IndexOf(x, comparisonType) >= 0);
#else
        return toCheck.Any(x => value.Contains(x, comparisonType));
#endif
    }

    /// <summary>
    /// Searches for the index of the first occurrence of the specified strings in the input string.
    /// </summary>
    /// <param name="value">The input string to search in.</param>
    /// <param name="matchCandidates">The strings to search for.</param>
    /// <returns>The index of the first occurrence of the specified strings, or -1 if no match was found.</returns>
    public static int IndexOf(this string value, params string[] matchCandidates)
    {
        foreach (var checkValue in matchCandidates)
        {
            var index = value.IndexOf(checkValue, StringComparison.Ordinal);

            if (index != -1)
                return index;
        }

        return -1;
    }

    /// <summary>
    /// Converts string to decimal value and replace symbols with CurrentCulture NumberDecimalSeparator
    /// </summary>
    /// <param name="value">Decimal number in string format</param>
    /// <returns>Decimal value with correct separator</returns>
    /// <exception cref="Exception">Throw an exception when string is in incorrect format</exception>
    public static decimal FromString(this string value)
    {
        var numberDecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        const string dot = ".";
        const string comma = ",";

        if (value.Contains(dot)
            && dot != numberDecimalSeparator)
            value = value.Replace(dot, numberDecimalSeparator);
        else if (value.Contains(comma)
                 && comma != numberDecimalSeparator)
            value = value.Replace(comma, numberDecimalSeparator);

        return decimal.TryParse(value, out var result)
            ? result
            : throw new("Cannot unmarshal type decimal");
    }

    /// <summary>Converts a string to a <see cref="Uri" />.</summary>
    /// <param name="source">The string to convert.</param>
    /// <param name="baseUri">When set, <paramref name="source" /> is resolved relative to this URI.</param>
    /// <param name="kind">The kind of URI to create when no base URI is given.</param>
    /// <returns>The URI, or null when the string is blank or not a valid URI.</returns>
    public static Uri? ToUri(this string source, Uri? baseUri = null, UriKind kind = UriKind.Absolute)
    {
        if (source?.IsNotNullOrEmptyOrWhiteSpace() != true)
            return null;

        return baseUri switch
        {
            not null => new(baseUri, source),
            _ => Uri.TryCreate(source, kind, out var result)
                ? result
                : null
        };
    }

    /// <summary>
    /// Modulus 10 algorithm created by Hans Peter Luhn. Uses a weight of 2 which
    /// is applied to every odd position digit.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     Valid characters are decimal digits (0-9).
    ///     </para>
    ///     <para>
    ///     Check digit calculated by the algorithm is a decimal digit (0-9).
    ///     </para>
    ///     <para>
    ///     Assumes that the check digit (if present) is the right-most digit in the
    ///     input value.
    ///     </para>
    ///     <para>
    ///     Will detect all single-digit transcription errors and most two digit
    ///     transpositions of adjacent digits (except 09 - 90). Will detect most
    ///     twin errors (i.e. 11 - 44) except 22 - 55,  33 - 66 and 44 - 77.
    ///     </para>
    /// </remarks>
    /// <param name="value">Value to check</param>
    /// <returns>True if provided string contains valid number</returns>
    public static bool ValidateCheckDigit(this string value)
    {
        const char zeroChar = '0';

        if (string.IsNullOrEmpty(value)
            || value.Length < 2
            || value.Any(static element => !char.IsDigit(element)))
            return false;

        var sum = 0;
        var shouldApplyDouble = true;

        for (var index = value.Length - 2; index >= 0; index--)
        {
            var currentDigit = value[index] - zeroChar;

            if (currentDigit is < 0 or > 9)
                return false;

            sum += shouldApplyDouble
                ? _doubledValues[currentDigit]
                : currentDigit;

            shouldApplyDouble = !shouldApplyDouble;
        }

        var checkDigit = (10 - sum % 10) % 10;

        return
#if NETSTANDARD2_0
            value[value.Length - 1]
#else
            value[^1]
#endif
            - zeroChar
            == checkDigit;
    }

    /// <summary>
    /// Converts a string to proper case, handling special cases for names, Scottish prefixes, and Roman numerals.
    /// </summary>
    /// <param name="input">The input string to convert to proper case.</param>
    /// <returns>String converted to proper case with special handling for names.</returns>
    public static string ToProperCase(this string input)
    {
        if (input.IsAllUpperOrAllLower())
            // fix the ALL UPPERCASE or all lowercase names
            return string.Join(" ", input.Split(' ').Select(StringUtilities.WordToProperCase));

        // leave the CamelCase or Propercase names alone
        return input;
    }

    /// <summary>Determines whether the string equals both its lower-case and upper-case form, or at least one of them.</summary>
    /// <param name="input">The string to test.</param>
    /// <returns><c>true</c> if the string is entirely upper-case or entirely lower-case; otherwise <c>false</c>.</returns>
    public static bool IsAllUpperOrAllLower(this string input) =>
        input.ToLower().Equals(input) || input.ToUpper().Equals(input);

    /// <summary>Compares two strings ignoring case with the ordinal comparer.</summary>
    /// <param name="source">The first string.</param>
    /// <param name="value">The second string.</param>
    /// <returns><c>true</c> if the strings are equal ignoring case.</returns>
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
    public static bool IsNullOrEmptyString([NotNullWhen(false)] this string? value) =>
        value is null || string.IsNullOrEmpty(value);

    /// <summary>
    /// Gets a value indicating if the string is NOT Null or Empty.
    /// </summary>
    /// <param name="value">string to test.</param>
    /// <returns>True if string is Null or Empty otherwise False.</returns>
    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyString([NotNullWhen(true)] this string? value) =>
        value is not null && !string.IsNullOrEmpty(value);

    /// <summary>
    /// Removes the specified chars from current string.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="chars">The chars.</param>
    /// <returns>String with chars removed.</returns>
    public static string Remove(this string text, params char[] chars) =>
        chars.Aggregate(text, (current, c) => current.Replace(c.ToString(), string.Empty));

    /// <summary>
    /// Removes the specified strings from current string.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="strings">The strings.</param>
    /// <returns>String with strings removed.</returns>
    public static string Remove(this string text, params string[] strings) =>
        strings.Aggregate(text, (current, c) => current.Replace(c, string.Empty));

    /// <summary>
    /// Writes value to the debug output.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Value passed.</returns>
    public static string ToDebug(this string value)
    {
        Debug.WriteLine(value);

        return value;
    }

    /// <summary>
    /// Writes value to the trace output.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Value passed.</returns>
    public static string ToTrace(this string value)
    {
        Trace.WriteLine(value);

        return value;
    }

    /// <summary>
    /// Determines whether given string has no wild cards.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>True if string contains wild cards; otherwise false.</returns>
    public static bool HasNoWildCards(this string value) =>
        !value.Contains(StandardWildCardAnyValue) && !value.Contains(StandardWildCardOneCharacter);

    /// <summary>
    /// Determines whether given string has no SQL wild cards.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>True if string contains SQL wild cards; otherwise false.</returns>
    public static bool HasNoSqlWildCards(this string value) =>
        !value.Contains(SqlWildCardAnyValue) && !value.Contains(SqlWildCardOneCharacter);

    /// <summary>
    /// Replaces the standard wild cards by SQL ones.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>String with replaced wild cards.</returns>
    public static string ReplaceStandardWildCardsBySql(this string value) =>
        value.Replace(StandardWildCardAnyValue, SqlWildCardAnyValue)
            .Replace(StandardWildCardOneCharacter, SqlWildCardOneCharacter);

    /// <summary>
    /// Escape SQL wild cards characters.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>String with escaped wild card characters.</returns>
    public static string EscapeSqlWildCards(this string value) =>
        value.Replace(SqlWildCardAnyValue.ToString(), SqlWildCardAnyValueEscaped)
            .Replace(SqlWildCardOneCharacter.ToString(), SqlWildCardOneCharacterEscaped);

    /// <summary>
    /// Splits the specified string into parts.
    /// </summary>
    /// <param name="value">The value to split.</param>
    /// <param name="splitValue">The split value.</param>
    /// <param name="stringLength">Length of the string.</param>
    /// <returns>List of string chunks.</returns>
    public static IList<string> Split(this string value, string splitValue, int stringLength)
    {
        var chunks = new List<string>();
        var start = 0;

        while (start < value.Length)
        {
            if (start + stringLength >= value.Length)
            {
                chunks.Add(value.Substring(start));

                break;
            }

            var chunk = value.Substring(start, stringLength);
            var splitIndex = chunk.LastIndexOf(splitValue, StringComparison.Ordinal);

            if (splitIndex == -1
                || splitIndex == 0)
            {
                // No split found or at beginning, take the full chunk
                chunks.Add(chunk);
                start += stringLength;
            }
            else
            {
                // Split found, take up to and including the split
                chunks.Add(chunk.Substring(0, splitIndex + splitValue.Length));
                start += splitIndex + splitValue.Length;
            }
        }

        return chunks;
    }

    /// <summary>
    /// Converts the specified string to stream.
    /// </summary>
    /// <param name="str">The string.</param>
    /// <returns>Stream of the string.</returns>
    public static Stream ToStream(this string str)
    {
        var stream = new MemoryStream();
#pragma warning disable IDISP001 // StreamWriter must not be disposed - it would close the returned stream
        var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true);
#pragma warning restore IDISP001
        writer.Write(str);
        writer.Flush();
        stream.Position = 0;

        return stream;
    }

    /// <summary>
    /// Divides the by capital letter.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Value divided by capital letter.</returns>
    public static string DivideByCapital(this string value) => Regex.Replace(value, "([A-Z])", " $1").TrimStart(' ');

    /// <summary>
    /// Determines whether the specified string is null or white space.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> if the specified string is null or white space; otherwise, <c>false</c>.</returns>
    [ContractAnnotation("null => true")]
    public static bool IsNullOrWhiteSpace(this string value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Determines whether the specified string is not null or white space.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> if the specified string is not null or white space; otherwise, <c>false</c>.</returns>
    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrWhiteSpace(this string value) => !string.IsNullOrWhiteSpace(value);

    /// <summary>Determines whether a string is neither null, empty nor white space.</summary>
    /// <param name="value">The string to test.</param>
    /// <returns><c>true</c> if the string has at least one non-white-space character.</returns>
    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyOrWhiteSpace(this string value) =>
        value.IsNotNullOrEmptyString() && value.IsNotNullOrWhiteSpace();

    /// <summary>Determines whether a string is null, empty or consists only of white space.</summary>
    /// <param name="value">The string to test.</param>
    /// <returns><c>true</c> if the string is null, empty or white space.</returns>
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmptyOrWhiteSpace(this string value) =>
        value.IsNullOrEmptyString() || value.IsNullOrWhiteSpace();

    /// <summary>Determines whether the whole text is matched by a regular expression pattern.</summary>
    /// <param name="text">The text to test.</param>
    /// <param name="regexPattern">The regular expression pattern.</param>
    /// <returns><c>true</c> if the first match spans the entire text.</returns>
    public static bool MatchesRegex(this string text, string regexPattern) =>
        text.MatchesRegex(new Regex(regexPattern));

    /// <summary>Determines whether the whole text is matched by a regular expression.</summary>
    /// <param name="text">The text to test.</param>
    /// <param name="regex">The regular expression.</param>
    /// <returns><c>true</c> if the first match spans the entire text.</returns>
    public static bool MatchesRegex(this string text, Regex regex)
    {
        var match = regex.Match(text);

        return match.Value.Equals(text);
    }

    /// <summary>Determines whether the whole text is a single word made of word characters and apostrophes.</summary>
    /// <param name="text">The text to test.</param>
    /// <returns><c>true</c> if the text is a word.</returns>
    public static bool IsAWord(this string text) => text.MatchesRegex(WordRegex);

    /// <summary>Determines whether the whole text consists only of ASCII letters.</summary>
    /// <param name="text">The text to test.</param>
    /// <returns><c>true</c> if the text contains only letters.</returns>
    public static bool ContainsOnlyLetters(this string text) => text.MatchesRegex(LettersRegex);

    /// <summary>Determines whether the whole text consists only of ASCII letters and digits.</summary>
    /// <param name="text">The text to test.</param>
    /// <returns><c>true</c> if the text contains only letters and numbers.</returns>
    public static bool ContainsOnlyLettersAndNumbers(this string text) => text.MatchesRegex(LettersAndNumbersRegex);

    /// <summary>Determines whether the whole text consists only of ASCII letters, digits and underscores.</summary>
    /// <param name="text">The text to test.</param>
    /// <returns><c>true</c> if the text contains only letters, numbers and underscores.</returns>
    public static bool ContainsOnlyLettersNumbersAndUnderscore(this string text) =>
        text.MatchesRegex(LettersNumbersAndUnderscoreRegex);

    /// <summary>Determines whether a string contains another string using the given comparison.</summary>
    /// <param name="source">The string to search.</param>
    /// <param name="toCheck">The string to look for.</param>
    /// <param name="comp">The string comparison to use.</param>
    /// <returns><c>true</c> if <paramref name="toCheck" /> occurs in <paramref name="source" />; <c>false</c> when the source is null or there is no match.</returns>
    public static bool Contains(this string source, string toCheck, StringComparison comp) =>
        source?.IndexOf(toCheck, comp) >= 0;

    /// <summary>Splits a path into its segments on both directory separator characters.</summary>
    /// <param name="path">The path to split.</param>
    /// <returns>The path segments.</returns>
    public static IEnumerable<string> GetPathParts(this string path) =>
        path.Split(Path.DirectorySeparatorChar).SelectMany(x => x.Split(Path.AltDirectorySeparatorChar));

    /// <summary>Upper-cases the first character of a string.</summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The string with its first character upper-cased.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="input" /> is empty.</exception>
    public static string FirstCharToUpper(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
#if NETSTANDARD
            _ => input[0].ToString().ToUpper() + input.Substring(1)
#else
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
#endif
        };

    /// <summary>Lower-cases the first character of a string.</summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The string with its first character lower-cased.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="input" /> is empty.</exception>
    public static string FirstCharToLower(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
#if NETSTANDARD
            _ => input[0].ToString().ToLower() + input.Substring(1)
#else
            _ => string.Concat(input[0].ToString().ToLower(), input.AsSpan(1))
#endif
        };

    /// <summary>Returns a string containing a specified number of characters from the left side of a string.</summary>
    /// <param name="str">Required. <see langword="String" /> expression from which the leftmost characters are returned.</param>
    /// <param name="length">
    /// Required. <see langword="Integer" /> expression. Numeric expression indicating how many characters
    /// to return. If 0, a zero-length string ("") is returned. If greater than or equal to the number of characters in
    /// <paramref name="str" />, the entire string is returned.
    /// </param>
    /// <param name="trim">Trims provided string before processing.</param>
    /// <returns>Returns a string containing a specified number of characters from the left side of a string.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="length" /> { 0.
    /// </exception>
    public static string Left(this string str, int length, bool trim = false)
    {
        if (trim)
            str = str.Trim();

#if NETSTANDARD
        return str.Substring(0, length);
#else
        return str[..length];
#endif
    }

    /// <summary>Returns a string that contains all the characters starting from a specified position in a string.</summary>
    /// <param name="str">Required. <see langword="String" /> expression from which characters are returned.</param>
    /// <param name="start">
    /// Required. <see langword="Integer" /> expression. Starting position of the characters to return. If
    /// <paramref name="start" /> is greater than the number of characters in <paramref name="str" />, the
    /// <see langword="Mid" /> function returns a zero-length string (""). <paramref name="start" /> is one-based.
    /// </param>
    /// <param name="trim">Trims provided string before processing.</param>
    /// <returns>A string that consists of all the characters starting from the specified position in the string.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="start" /> {= 0.
    /// </exception>
    public static string Mid(this string str, int start, bool trim = false)
    {
        if (trim)
            str = str.Trim();

#if NETSTANDARD
        return str.Substring(start);
#else
        return str[start..];
#endif
    }

    /// <summary>
    /// Returns a string that contains a specified number of characters starting from a specified position in a
    /// string.
    /// </summary>
    /// <param name="str">Required. <see langword="String" /> expression from which characters are returned.</param>
    /// <param name="start">
    /// Required. <see langword="Integer" /> expression. Starting position of the characters to return. If
    /// <paramref name="start" /> is greater than the number of characters in <paramref name="str" />, the
    /// <see langword="Mid" /> function returns a zero-length string (""). <paramref name="start" /> is one based.
    /// </param>
    /// <param name="length">
    /// Optional. <see langword="Integer" /> expression. Number of characters to return. If omitted or if
    /// there are fewer than <paramref name="length" /> characters in the text (including the character at position
    /// <paramref name="start" />), all characters from the start position to the end of the string are returned.
    /// </param>
    /// <param name="trim">Trims provided string before processing.</param>
    /// <returns>
    /// A string that consists of the specified number of characters starting from the specified position in the
    /// string.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="start" /> {= 0 or <paramref name="length" /> { 0.
    /// </exception>
    public static string Mid(this string str, int start, int length, bool trim = false)
    {
        if (trim)
            str = str.Trim();

        return str.Substring(start, length);
    }

    /// <summary>Returns a string containing a specified number of characters from the right side of a string.</summary>
    /// <param name="str">Required. <see langword="String" /> expression from which the rightmost characters are returned.</param>
    /// <param name="length">
    /// Required. <see langword="Integer" />. Numeric expression indicating how many characters to return.
    /// If 0, a zero-length string ("") is returned. If greater than or equal to the number of characters in
    /// <paramref name="str" />, the entire string is returned.
    /// </param>
    /// <param name="trim">Trims provided string before processing.</param>
    /// <returns>Returns a string containing a specified number of characters from the right side of a string.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="length" /> { 0.
    /// </exception>
    public static string Right(this string str, int length, bool trim = false)
    {
        if (trim)
            str = str.Trim();

        return str.Substring(str.Length - length, length);
    }

    /// <summary>Gets the length of a string, optionally ignoring leading and trailing white space.</summary>
    /// <param name="value">The string to measure.</param>
    /// <param name="trim">When true, the string is trimmed before measuring.</param>
    /// <returns>The string length.</returns>
    public static int Length(this string value, bool trim = true)
    {
        if (trim)
            value = value.Trim();

        return value.Length;
    }

    /// <summary>Encodes a string as Base64.</summary>
    /// <param name="str">The string to encode.</param>
    /// <param name="enc">The encoding used to get the bytes; UTF-8 when null.</param>
    /// <returns>The Base64 representation.</returns>
    public static string ToBase64(this string str, Encoding? enc = null)
    {
        enc ??= Encoding.UTF8;

        return Convert.ToBase64String(enc.GetBytes(str));
    }

    /// <summary>Decodes a Base64 string.</summary>
    /// <param name="base64EncodedData">The Base64 text to decode.</param>
    /// <param name="enc">The encoding used to read the bytes; UTF-8 when null.</param>
    /// <returns>The decoded string.</returns>
    public static string FromBase64(this string base64EncodedData, Encoding? enc = null)
    {
        enc ??= Encoding.UTF8;

        return enc.GetString(Convert.FromBase64String(base64EncodedData));
    }

    /// <summary>Copies all remaining bytes from one stream to another using a 4 KB buffer.</summary>
    /// <param name="src">The stream to read from.</param>
    /// <param name="dest">The stream to write to.</param>
    public static void CopyTo(this Stream src, Stream dest)
    {
        var bytes = new byte[4096];
        int cnt;

        while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            dest.Write(bytes, 0, cnt);
    }

    /// <summary>Compresses a string with GZip.</summary>
    /// <param name="str">The string to compress, encoded as UTF-8.</param>
    /// <returns>The compressed bytes as a Base64 string.</returns>
    public static string Zip(this string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);

        using var msi = new MemoryStream(bytes);
        using var mso = new MemoryStream();
        using var gs = new GZipStream(mso, CompressionMode.Compress);
        CopyTo(msi, gs);

        return Convert.ToBase64String(mso.ToArray());
    }

    /// <summary>Decompresses a Base64 GZip string produced by <see cref="Zip(string)" />.</summary>
    /// <param name="bytes">The Base64 compressed data.</param>
    /// <returns>The decompressed UTF-8 string.</returns>
    public static string Unzip(this string bytes)
    {
        using var msi = new MemoryStream(Convert.FromBase64String(bytes));
        using var mso = new MemoryStream();
        using var gs = new GZipStream(msi, CompressionMode.Decompress);
        CopyTo(gs, mso);

        return Encoding.UTF8.GetString(mso.ToArray());
    }

    /// <summary>Compares two strings, treating two nulls as equal.</summary>
    /// <param name="source">The first string.</param>
    /// <param name="value">The second string.</param>
    /// <param name="comparisonType">The comparison to use.</param>
    /// <returns><c>true</c> if both are null or the strings are equal.</returns>
    public static bool IsBothNullOrEqual(this string source,
                                         string value,
                                         StringComparison comparisonType = StringComparison.Ordinal) =>
        source is null && value is null || source?.Equals(value, comparisonType) == true;

    /// <summary>Converts bytes to a lower-case hexadecimal string without separators.</summary>
    /// <param name="ba">The bytes to convert.</param>
    /// <returns>The hexadecimal text.</returns>
    public static string ByteArrayToString(this byte[] ba)
    {
        var hex = new StringBuilder(ba.Length * 2);

        foreach (var b in ba)
            hex.Append($"{b:x2}");

        return hex.ToString();
    }

    /// <summary>Converts a hexadecimal string to a byte array.</summary>
    /// <param name="hex">A string of hexadecimal digit pairs.</param>
    /// <returns>The decoded bytes.</returns>
    public static byte[] StringToByteArray(this string hex)
    {
        var numberChars = hex.Length;
        var bytes = new byte[numberChars / 2];

        for (var i = 0; i < numberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

        return bytes;
    }

    /// <summary>Formats a nullable GUID as an upper-case string in braces.</summary>
    /// <param name="guid">The GUID to format.</param>
    /// <returns>The formatted GUID, or null when it has no value.</returns>
    public static string? GetGuidString(this Guid? guid) =>
        guid.HasValue
            ? guid.Value.GetGuidString()
            : null;

    /// <summary>Formats a GUID as an upper-case string in braces.</summary>
    /// <param name="guid">The GUID to format.</param>
    /// <returns>The formatted GUID, for example <c>{3F2504E0-...}</c>.</returns>
    public static string GetGuidString(this Guid guid) => $"{{{guid.ToString().ToUpper()}}}";

    /// <summary>Converts every line break (<c>\n</c>, <c>\r</c> or <c>\r\n</c>) to <c>\r\n</c>.</summary>
    /// <param name="input">The text to normalize.</param>
    /// <returns>The text with consistent CRLF line breaks.</returns>
    public static string NormalizeLineBreaks(this string input)
    {
        // Allow 10% as a rough guess of how much the string may grow.
        // If we're wrong we'll either waste space or have extra copies -
        // it will still work
        var builder = new StringBuilder((int)(input.Length * 1.1));

        var lastWasCR = false;

        foreach (var c in input)
        {
            if (lastWasCR)
            {
                lastWasCR = false;

                if (c == '\n')
                    continue; // Already written \r\n
            }

            switch (c)
            {
                case '\r':
                    builder.Append("\r\n");
                    lastWasCR = true;

                    break;
                case '\n':
                    builder.Append("\r\n");

                    break;
                default:
                    builder.Append(c);

                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>Computes the SHA-256 hash of the UTF-8 bytes of a string.</summary>
    /// <param name="rawData">The string to hash.</param>
    /// <returns>The hash as a lower-case hexadecimal string.</returns>
    public static string ComputeSha256Hash(this string rawData)
    {
        var bytes = Encoding.UTF8.GetBytes(rawData);
#if NETSTANDARD
        using var sha256Hash = SHA256.Create();
        var hash = sha256Hash.ComputeHash(bytes);
#else
        var hash = SHA256.HashData(bytes);
#endif
        return hash.ByteArrayToString();
    }

    /// <summary>Truncates a string to a maximum length.</summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="length">The maximum number of characters to keep.</param>
    /// <param name="trim">When true, the string is trimmed before truncating.</param>
    /// <returns>The original string if it is not longer than <paramref name="length" />; otherwise its first <paramref name="length" /> characters.</returns>
    public static string TrimLength(this string value, int length, bool trim = false)
    {
        if (trim)
            value = value.Trim();

        if (value.Length <= length)
            return value;

#if NETSTANDARD
        return value.Substring(0, length);
#else
        return value[..length];
#endif
    }

    /// <summary>
    /// Generates the md5 of string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>MD5 of string</returns>
    public static string? GenerateMd5OfString(this string value)
    {
        if (value is null)
            return null;

        using var stream = value.ToStream();

#if NETSTANDARD
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(stream);
#else
        var hash = MD5.HashData(stream);
#endif
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
    }

    /// <summary>Removes diacritical marks (accents) from a string.</summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>The text with combining marks removed, in normalization form C.</returns>
    public static string RemoveDiacritics(this string text)
    {
        var formD = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var ch in formD.Select(ch => new
                     {
                         ch,
                         uc = CharUnicodeInfo.GetUnicodeCategory(ch)
                     })
                     .Where(t => t.uc != UnicodeCategory.NonSpacingMark)
                     .Select(t => t.ch))
            sb.Append(ch);

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Converts string to int, returns -1 if conversion fails
    /// </summary>
    /// <param name="value">String value to convert</param>
    /// <returns>Converted integer or -1 if conversion fails</returns>
    public static int ToInt(this string value) =>
        int.TryParse(value, out var result)
            ? result
            : -1;
}