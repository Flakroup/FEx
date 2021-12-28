using JetBrains.Annotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FEx.Extensions;

/// <summary>
///     String extensions class.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     Wild card position.
    /// </summary>
    public enum WildCardPosition
    {
        Start,
        End,
        Middle,
        StartAndEnd
    }

    /// <summary>
    ///     The standard wild card 'any value'.
    /// </summary>
    public const char StandardWildCardAnyValue = '*';

    /// <summary>
    ///     The SQL wild card 'any value'.
    /// </summary>
    public const char SqlWildCardAnyValue = '%';

    /// <summary>
    ///     The SQL wild card 'any value'.
    /// </summary>
    public const string SqlWildCardAnyValueEscaped = "[%]";

    /// <summary>
    ///     The standard wild card 'any value'.
    /// </summary>
    public const char StandardWildCardOneCharacter = '?';

    /// <summary>
    ///     The SQL wild card 'any value'.
    /// </summary>
    public const char SqlWildCardOneCharacter = '_';

    /// <summary>
    ///     The SQL wild card 'any value'.
    /// </summary>
    public const string SqlWildCardOneCharacterEscaped = "[_]";

    /// <summary>
    ///     Removes the specified chars from current string.
    /// </summary>
    /// <param name="source">Current string.</param>
    /// <param name="chars">The chars to remove.</param>
    /// <returns>A string.</returns>
    public static string Remove(this string source, IEnumerable<char> chars)
    {
        return new string(source.Where(c => !chars.Contains(c)).ToArray());
    }

    /// <summary>
    ///     Compare 2 strings, ignoring case.
    /// </summary>
    /// <param name="source">First value to compare with.</param>
    /// <param name="value">Second value to compare with.</param>
    /// <returns>True if equal otherwise False.</returns>
    /// <remarks></remarks>
    public static bool EqualsIgnoreCase(this string source, string value)
    {
        return string.Equals(source, value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Gets a value indicating if the string is Null or Empty.
    /// </summary>
    /// <param name="value">string to test.</param>
    /// <returns>True if string is Null or Empty otherwise False.</returns>
    /// <remarks></remarks>
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmptyString(this string value)
    {
        return value == null || string.IsNullOrEmpty(value);
    }

    /// <summary>
    ///     Gets a value indicating if the string is NOT Null or Empty.
    /// </summary>
    /// <param name="value">string to test.</param>
    /// <returns>True if string is Null or Empty otherwise False.</returns>
    /// <remarks></remarks>
    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyString(this string value)
    {
        return value != null && !string.IsNullOrEmpty(value);
    }

    /// <summary>
    ///     Formats the value with the parameters using string.Format.
    /// </summary>
    /// <param name="value">The input string.</param>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public static string FormatWith(this string value, params object[] parameters)
    {
        return string.Format(value, parameters);
    }

    /// <summary>
    ///     Gets a int from a string.
    /// </summary>
    /// <param name="value">string with number.</param>
    /// <returns>-1 if value is (Null or Empty or not Numeric) otherwise the number.</returns>
    /// <remarks></remarks>
    public static int ToInt(this string value)
    {
        return int.TryParse(value, out int result) ? result : -1;
    }

    public static int? ToNullableInt(this string value)
    {
        return value != null ? int.TryParse(value, out int result) ? result : null : null;
    }

    /// <summary>
    ///     Gets a int from a string.
    /// </summary>
    /// <param name="value">string with number.</param>
    /// <param name="defaultResult">Number to return if parse fail.</param>
    /// <returns>defaultResult if value is (Null or Empty or not Numeric) otherwise the number.</returns>
    /// <remarks></remarks>
    public static int ToInt(this string value, int defaultResult)
    {
        return int.TryParse(value, out int result) ? result : defaultResult;
    }

    /// <summary>
    ///     Writes an unformatted string to the Trace output.
    /// </summary>
    /// <param name="value">string to output.</param>
    public static string ToTrace(this string value)
    {
        Trace.WriteLine(value);
        return value;
    }

    /// <summary>
    ///     Determines whether given string has no wild cards.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>True if string contains wild cards; otherwise false.</returns>
    public static bool HasNoWildCards(this string value)
    {
        return !value.Contains(StandardWildCardAnyValue) && !value.Contains(StandardWildCardOneCharacter);
    }

    /// <summary>
    ///     Determines whether given string has no SQL wild cards.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>True if string contains SQL wild cards; otherwise false.</returns>
    public static bool HasNoSqlWildCards(this string value)
    {
        return !value.Contains(SqlWildCardAnyValue) && !value.Contains(SqlWildCardOneCharacter);
    }

    /// <summary>
    ///     Replaces the standard wild cards by SQL ones.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>String with replaced wild cards.</returns>
    public static string ReplaceStandardWildCardsBySql(this string value)
    {
        return value.Replace(StandardWildCardAnyValue, SqlWildCardAnyValue).Replace(StandardWildCardOneCharacter, SqlWildCardOneCharacter);
    }

    /// <summary>
    ///     Escape SQL wild cards characters.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>String with escaped wild card characters.</returns>
    public static string EscapeSqlWildCards(this string value)
    {
        return value.Replace(SqlWildCardAnyValue.ToString(), SqlWildCardAnyValueEscaped).Replace(SqlWildCardOneCharacter.ToString(), SqlWildCardOneCharacterEscaped);
    }

    /// <summary>
    ///     Splits the specified string into parts.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="elementLength">Length of the element.</param>
    /// <returns>Value splitted into parts.</returns>
    public static IEnumerable<string> Split(this string value, int elementLength)
    {
        int fullLength = value.Length;
        IList<string> elements = new List<string>();
        for (var startIndex = 0; startIndex < value.Length; startIndex += elementLength)
        {
            if (startIndex + elementLength > fullLength)
            {
                elementLength = fullLength - startIndex;
            }

            elements.Add(value.Substring(startIndex, elementLength));
        }

        return elements;
    }

    /// <summary>
    ///     Gets the splited element.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="separator">The separator.</param>
    /// <param name="index">The index.</param>
    /// <returns>The splited element.</returns>
    public static string GetSplitedElement(this string value, char separator, int index)
    {
        return !string.IsNullOrWhiteSpace(value) ? value.Split(separator)[index] : string.Empty;
    }

    /// <summary>
    ///     Creates stream from the string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The stream.</returns>
    [SuppressMessage("Wrong Usage", "DF0010:Marks undisposed local variables.")]
    public static Stream ToStream(this string value)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(value);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    ///     Divides the by capital letter.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Value divided by capital letter.</returns>
    public static string DivideByCapital(this string value)
    {
        return Regex.Replace(value, "([A-Z])", " $1").TrimStart(' ');
    }

    /// <summary>
    ///     Determines whether the specified string is null or white space.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> if the specified string is null or white space; otherwise, <c>false</c>.</returns>
    [ContractAnnotation("null => true")]
    public static bool IsNullOrWhiteSpace(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    ///     Determines whether the specified string is not null or white space.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> if the specified string is not null or white space; otherwise, <c>false</c>.</returns>
    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrWhiteSpace(this string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyOrWhiteSpace(this string value)
    {
        return value.IsNotNullOrEmptyString() && value.IsNotNullOrWhiteSpace();
    }

    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmptyOrWhiteSpace(this string value)
    {
        return value.IsNullOrEmptyString() || value.IsNullOrWhiteSpace();
    }

    public static bool MatchesRegex(this string text, string regexPattern)
    {
        return MatchesRegex(text, new Regex(regexPattern));
    }

    public static bool MatchesRegex(this string text, Regex regex)
    {
        Match match = regex.Match(text);
        return match.Value.Equals(text);
    }

    public static bool IsAWord(this string text)
    {
        return text.MatchesRegex(new Regex(@"\b[\w']+\b"));
    }

    public static bool ContainsOnlyLetters(this string text)
    {
        return text.MatchesRegex(new Regex("^[a-zA-Z]+$"));
    }

    public static bool ContainsOnlyLettersAndNumbers(this string text)
    {
        return text.MatchesRegex(new Regex("^[a-zA-Z0-9]+$"));
    }

    public static bool ContainsOnlyLettersNumbersAndUnderscore(this string text)
    {
        return text.MatchesRegex(new Regex("^[a-zA-Z0-9_]+$"));
    }

    public static bool Contains(this string source, string toCheck, StringComparison comp)
    {
        return source?.IndexOf(toCheck, comp) >= 0;
    }

    public static IEnumerable<string> GetPathParts(this string path)
    {
        return path.Split(Path.DirectorySeparatorChar).SelectMany(x => x.Split(Path.AltDirectorySeparatorChar));
    }

    public static string FirstCharToUpper(this string input)
    {
        switch (input)
        {
            case null:
                throw new ArgumentNullException(nameof(input));
            case "":
                throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
            default:
                return input[0].ToString().ToUpper() + input.Substring(1);
        }
    }

    public static string FirstCharToLower(this string input)
    {
        switch (input)
        {
            case null:
                throw new ArgumentNullException(nameof(input));
            case "":
                throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
            default:
                return input[0].ToString().ToLower() + input.Substring(1);
        }
    }

    /// <summary>Returns a string containing a specified number of characters from the left side of a string.</summary>
    /// <param name="str">Required. <see langword="String" /> expression from which the leftmost characters are returned.</param>
    /// <param name="length">
    ///     Required. <see langword="Integer" /> expression. Numeric expression indicating how many characters
    ///     to return. If 0, a zero-length string ("") is returned. If greater than or equal to the number of characters in
    ///     <paramref name="str" />, the entire string is returned.
    /// </param>
    /// <param name="trim">Trims provided string before processing.</param>
    /// <returns>Returns a string containing a specified number of characters from the left side of a string.</returns>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="length" /> { 0.
    /// </exception>
    public static string Left(this string str, int length, bool trim = false)
    {
        if (trim)
        {
            str = str.Trim();
        }

        return str.Substring(0, length);
    }

    /// <summary>Returns a string that contains all the characters starting from a specified position in a string.</summary>
    /// <param name="str">Required. <see langword="String" /> expression from which characters are returned.</param>
    /// <param name="start">
    ///     Required. <see langword="Integer" /> expression. Starting position of the characters to return. If
    ///     <paramref name="start" /> is greater than the number of characters in <paramref name="str" />, the
    ///     <see langword="Mid" /> function returns a zero-length string (""). <paramref name="start" /> is one-based.
    /// </param>
    /// <param name="trim">Trims provided string before processing.</param>
    /// <returns>A string that consists of all the characters starting from the specified position in the string.</returns>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="start" /> {= 0.
    /// </exception>
    public static string Mid(this string str, int start, bool trim = false)
    {
        if (trim)
        {
            str = str.Trim();
        }

        return str.Substring(start);
    }

    /// <summary>
    ///     Returns a string that contains a specified number of characters starting from a specified position in a
    ///     string.
    /// </summary>
    /// <param name="str">Required. <see langword="String" /> expression from which characters are returned.</param>
    /// <param name="start">
    ///     Required. <see langword="Integer" /> expression. Starting position of the characters to return. If
    ///     <paramref name="start" /> is greater than the number of characters in <paramref name="str" />, the
    ///     <see langword="Mid" /> function returns a zero-length string (""). <paramref name="start" /> is one based.
    /// </param>
    /// <param name="length">
    ///     Optional. <see langword="Integer" /> expression. Number of characters to return. If omitted or if
    ///     there are fewer than <paramref name="length" /> characters in the text (including the character at position
    ///     <paramref name="start" />), all characters from the start position to the end of the string are returned.
    /// </param>
    /// <param name="trim">Trims provided string before processing.</param>
    /// <returns>
    ///     A string that consists of the specified number of characters starting from the specified position in the
    ///     string.
    /// </returns>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="start" /> {= 0 or <paramref name="length" /> { 0.
    /// </exception>
    public static string Mid(this string str, int start, int length, bool trim = false)
    {
        if (trim)
        {
            str = str.Trim();
        }

        return str.Substring(start, length);
    }

    /// <summary>Returns a string containing a specified number of characters from the right side of a string.</summary>
    /// <param name="str">Required. <see langword="String" /> expression from which the rightmost characters are returned.</param>
    /// <param name="length">
    ///     Required. <see langword="Integer" />. Numeric expression indicating how many characters to return.
    ///     If 0, a zero-length string ("") is returned. If greater than or equal to the number of characters in
    ///     <paramref name="str" />, the entire string is returned.
    /// </param>
    /// <param name="trim">Trims provided string before processing.</param>
    /// <returns>Returns a string containing a specified number of characters from the right side of a string.</returns>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="length" /> { 0.
    /// </exception>
    public static string Right(this string str, int length, bool trim = false)
    {
        if (trim)
        {
            str = str.Trim();
        }

        return str.Substring(str.Length - length, length);
    }

    public static int Length(this string value, bool trim = true)
    {
        if (trim)
        {
            value = value.Trim();
        }

        return value.Length;
    }

    public static string ToBase64(this string str, Encoding enc = null)
    {
        if (enc == null)
        {
            enc = Encoding.UTF8;
        }

        return Convert.ToBase64String(enc.GetBytes(str));
    }

    public static string FromBase64(this string base64EncodedData, Encoding enc = null)
    {
        if (enc == null)
        {
            enc = Encoding.UTF8;
        }

        return enc.GetString(Convert.FromBase64String(base64EncodedData));
    }

    public static void CopyTo(this Stream src, Stream dest)
    {
        var bytes = new byte[4096];

        int cnt;

        while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
        {
            dest.Write(bytes, 0, cnt);
        }
    }

    public static string Zip(this string str)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(str);

        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        using (var gs = new GZipStream(mso, CompressionMode.Compress))
        {
            CopyTo(msi, gs);

            return Convert.ToBase64String(mso.ToArray());
        }
    }

    public static string Unzip(this string bytes)
    {
        using (var msi = new MemoryStream(Convert.FromBase64String(bytes)))
        using (var mso = new MemoryStream())
        using (var gs = new GZipStream(msi, CompressionMode.Decompress))
        {
            CopyTo(gs, mso);

            return Encoding.UTF8.GetString(mso.ToArray());
        }
    }

    public static string ToProperCase(this string input)
    {
        if (input.IsAllUpperOrAllLower())
        {
            // fix the ALL UPPERCASE or all lowercase names
            return string.Join(" ", input.Split(' ').Select(ProperCaseHelper.WordToProperCase));
        }

        // leave the CamelCase or Propercase names alone
        return input;
    }

    public static bool CompareOrdinalIgnoreCase(this string source, string value)
    {
        return string.Compare(source, value, StringComparison.OrdinalIgnoreCase) == 0;
    }

    public static bool IsBothNullOrEqual(this string source, string value, StringComparison comparisonType = StringComparison.Ordinal)
    {
        return source == null && value == null || source?.Equals(value, comparisonType) == true;
    }

    public static Uri ToUri(this string source, Uri baseUri = null, UriKind kind = UriKind.Absolute)
    {
        if (source?.IsNotNullOrEmptyOrWhiteSpace() == true)
        {
            return baseUri != null
                ? new Uri(baseUri, source)
                : new Uri(source, kind);
        }

        return null;
    }

    public static string ByteArrayToString(this byte[] ba)
    {
        var hex = new StringBuilder(ba.Length * 2);

        foreach (byte b in ba)
        {
            hex.Append($"{b:x2}");
        }

        return hex.ToString();
    }

    public static byte[] StringToByteArray(this string hex)
    {
        int numberChars = hex.Length;
        var bytes = new byte[numberChars / 2];

        for (var i = 0; i < numberChars; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }

    public static string GetGuidString(this Guid? guid)
    {
        return guid.HasValue ? GetGuidString(guid.Value) : null;
    }

    public static string GetGuidString(this Guid guid)
    {
        return $"{{{guid.ToString().ToUpper()}}}";
    }

    public static string NormalizeLineBreaks(this string input)
    {
        // Allow 10% as a rough guess of how much the string may grow.
        // If we're wrong we'll either waste space or have extra copies -
        // it will still work
        var builder = new StringBuilder((int) (input.Length * 1.1));

        var lastWasCR = false;

        foreach (char c in input)
        {
            if (lastWasCR)
            {
                lastWasCR = false;
                if (c == '\n')
                {
                    continue; // Already written \r\n
                }
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
        using (var sha256Hash = SHA256.Create())
        {
            return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData)).ByteArrayToString();
        }
    }

    public static string TrimLength(this string value, int length, bool trim = false)
    {
        if (trim)
        {
            value = value.Trim();
        }

        return value.Length <= length ? value : value.Substring(0, length);
    }

    /// <summary>
    ///     Generates the md5 of string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>MD5 of string</returns>
    public static string GenerateMd5OfString(this string value)
    {
        if (value != null)
        {
            using (var md5 = MD5.Create())
            using (var stream = value.ToStream())
            {
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty).ToLower();
            }
        }

        return null;
    }
}