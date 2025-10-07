using FEx.Common.Extensions;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FEx.Extensions;

/// <summary>
/// String extensions class.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Wild card position.
    /// </summary>
    public enum WildCardPosition
    {
        Start,
        End,
        Middle,
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

    public static Regex WordRegex { get; } = new(@"\b[\w']+\b", RegexOptions.Compiled);
    public static Regex LettersRegex { get; } = new("^[a-zA-Z]+$", RegexOptions.Compiled);
    public static Regex LettersAndNumbersRegex { get; } = new("^[a-zA-Z0-9]+$", RegexOptions.Compiled);
    public static Regex LettersNumbersAndUnderscoreRegex { get; } = new("^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    /// <summary>
    /// Removes the specified chars from current string.
    /// </summary>
    /// <param name="source">Current string.</param>
    /// <param name="chars">The chars to remove.</param>
    /// <returns>A string.</returns>
    public static string Remove(this string source, IEnumerable<char> chars) =>
        new([.. source.Where(c => !chars.Contains(c))]);

    /// <summary>
    /// Formats the value with the parameters using string.Format.
    /// </summary>
    /// <param name="value">The input string.</param>
    /// <param name="parameters">The parameters.</param>
    public static string FormatWith(this string value, params object[] parameters) => string.Format(value, parameters);

    public static int? ToNullableInt(this string value) =>
        value is not null
            ? int.TryParse(value, out int result)
                ? result
                : null
            : null;

    /// <summary>
    /// Gets a int from a string.
    /// </summary>
    /// <param name="value">string with number.</param>
    /// <param name="fallback">Number to return if parse fail.</param>
    /// <returns>fallback if value is (Null or Empty or not Numeric) otherwise the number.</returns>
    public static int ToInt(this string value, int fallback = -1) =>
        int.TryParse(value, out int result)
            ? result
            : fallback;

    /// <summary>
    /// Writes an unformatted string to the Trace output.
    /// </summary>
    /// <param name="value">string to output.</param>
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
    /// <param name="value">The value.</param>
    /// <param name="elementLength">Length of the element.</param>
    /// <returns>Value splitted into parts.</returns>
    public static IEnumerable<string> Split(this string value, int elementLength)
    {
        int fullLength = value.Length;
        var elements = new List<string>();

        for (var startIndex = 0; startIndex < value.Length; startIndex += elementLength)
        {
            if (startIndex + elementLength > fullLength)
                elementLength = fullLength - startIndex;

            elements.Add(value.Substring(startIndex, elementLength));
        }

        return elements;
    }

    /// <summary>
    /// Gets the splited element.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="separator">The separator.</param>
    /// <param name="index">The index.</param>
    /// <returns>The splited element.</returns>
    public static string GetSplitedElement(this string value, char separator, int index) =>
        !string.IsNullOrWhiteSpace(value)
            ? value.Split(separator)[index]
            : string.Empty;

    /// <summary>
    /// Creates stream from the string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The stream.</returns>
    public static Stream ToStream(this string value)
    {
        var stream = new MemoryStream();
#pragma warning disable IDISP001
        var writer = new StreamWriter(stream);
#pragma warning restore IDISP001
        writer.Write(value);
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

    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyOrWhiteSpace(this string value) =>
        value.IsNotNullOrEmptyString() && value.IsNotNullOrWhiteSpace();

    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmptyOrWhiteSpace(this string value) =>
        value.IsNullOrEmptyString() || value.IsNullOrWhiteSpace();

    public static bool MatchesRegex(this string text, string regexPattern) =>
        MatchesRegex(text, new Regex(regexPattern));

    public static bool MatchesRegex(this string text, Regex regex)
    {
        Match match = regex.Match(text);

        return match.Value.Equals(text);
    }

    public static bool IsAWord(this string text) => text.MatchesRegex(WordRegex);

    public static bool ContainsOnlyLetters(this string text) => text.MatchesRegex(LettersRegex);

    public static bool ContainsOnlyLettersAndNumbers(this string text) => text.MatchesRegex(LettersAndNumbersRegex);

    public static bool ContainsOnlyLettersNumbersAndUnderscore(this string text) =>
        text.MatchesRegex(LettersNumbersAndUnderscoreRegex);

    public static bool Contains(this string source, string toCheck, StringComparison comp) =>
        source?.IndexOf(toCheck, comp) >= 0;

    public static IEnumerable<string> GetPathParts(this string path) =>
        path.Split(Path.DirectorySeparatorChar).SelectMany(x => x.Split(Path.AltDirectorySeparatorChar));

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
    /// <exception cref="T:System.ArgumentException">
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
    /// <exception cref="T:System.ArgumentException">
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
    /// <exception cref="T:System.ArgumentException">
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
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="length" /> { 0.
    /// </exception>
    public static string Right(this string str, int length, bool trim = false)
    {
        if (trim)
            str = str.Trim();

        return str.Substring(str.Length - length, length);
    }

    public static int Length(this string value, bool trim = true)
    {
        if (trim)
            value = value.Trim();

        return value.Length;
    }

    public static string ToBase64(this string str, Encoding enc = null)
    {
        enc ??= Encoding.UTF8;

        return Convert.ToBase64String(enc.GetBytes(str));
    }

    public static string FromBase64(this string base64EncodedData, Encoding enc = null)
    {
        enc ??= Encoding.UTF8;

        return enc.GetString(Convert.FromBase64String(base64EncodedData));
    }

    public static void CopyTo(this Stream src, Stream dest)
    {
        var bytes = new byte[4096];

        int cnt;

        while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            dest.Write(bytes, 0, cnt);
    }

    public static string Zip(this string str)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(str);

        using var msi = new MemoryStream(bytes);
        using var mso = new MemoryStream();
        using var gs = new GZipStream(mso, CompressionMode.Compress);
        CopyTo(msi, gs);

        return Convert.ToBase64String(mso.ToArray());
    }

    public static string Unzip(this string bytes)
    {
        using var msi = new MemoryStream(Convert.FromBase64String(bytes));
        using var mso = new MemoryStream();
        using var gs = new GZipStream(msi, CompressionMode.Decompress);
        CopyTo(gs, mso);

        return Encoding.UTF8.GetString(mso.ToArray());
    }

    public static bool IsBothNullOrEqual(this string source,
                                         string value,
                                         StringComparison comparisonType = StringComparison.Ordinal) =>
        source is null && value is null || source?.Equals(value, comparisonType) == true;

    public static Uri ToUri(this string source, Uri baseUri = null, UriKind kind = UriKind.Absolute) =>
        source?.IsNotNullOrEmptyOrWhiteSpace() == true
            ? baseUri is not null
                ? new(baseUri, source)
                : new Uri(source, kind)
            : null;

    public static string ByteArrayToString(this byte[] ba)
    {
        var hex = new StringBuilder(ba.Length * 2);

        foreach (byte b in ba)
            hex.Append($"{b:x2}");

        return hex.ToString();
    }

    public static byte[] StringToByteArray(this string hex)
    {
        int numberChars = hex.Length;
        var bytes = new byte[numberChars / 2];

        for (var i = 0; i < numberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

        return bytes;
    }

    public static string GetGuidString(this Guid? guid) =>
        guid.HasValue
            ? GetGuidString(guid.Value)
            : null;

    public static string GetGuidString(this Guid guid) => $"{{{guid.ToString().ToUpper()}}}";

    public static string NormalizeLineBreaks(this string input)
    {
        // Allow 10% as a rough guess of how much the string may grow.
        // If we're wrong we'll either waste space or have extra copies -
        // it will still work
        var builder = new StringBuilder((int)(input.Length * 1.1));

        var lastWasCR = false;

        foreach (char c in input)
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

    public static string ComputeSha256Hash(this string rawData)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(rawData);
#if NETSTANDARD
        using var sha256Hash = SHA256.Create();
        byte[] hash = sha256Hash.ComputeHash(bytes);
#else
        byte[] hash = SHA256.HashData(bytes);
#endif
        return hash.ByteArrayToString();
    }

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
    public static string GenerateMd5OfString(this string value)
    {
        if (value is null)
            return null;

        using var stream = value.ToStream();

#if NETSTANDARD
        using var md5 = MD5.Create();

        byte[] hash = md5.ComputeHash(stream);
#else
        byte[] hash = MD5.HashData(stream);
#endif
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
    }

    public static string RemoveDiacritics(this string text)
    {
        string formD = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (char ch in formD.Select(ch => new
                     {
                         ch,
                         uc = CharUnicodeInfo.GetUnicodeCategory(ch)
                     })
                     .Where(t => t.uc != UnicodeCategory.NonSpacingMark)
                     .Select(t => t.ch))
            sb.Append(ch);

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}