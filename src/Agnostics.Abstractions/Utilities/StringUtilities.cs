using System;
using System.Text.RegularExpressions;

namespace FEx.Agnostics.Abstractions.Utilities;

/// <summary>
/// String utility methods for advanced string processing.
/// </summary>
public static class StringUtilities
{
    public static Regex RomanNumeralsRegex { get; } = new(
        @"\b(?!Xi\b)(X|XX|XXX|XL|L|LX|LXX|LXXX|XC|C)?(I|II|III|IV|V|VI|VII|VIII|IX)?\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string WordToProperCase(string word)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        // Standard case
        var ret = CapitaliseFirstLetter(word);

        // Special cases:
        ret = ProperSuffix(ret, "'"); // D'Artagnon, D'Silva
        ret = ProperSuffix(ret, "."); // ???
        ret = ProperSuffix(ret, "-"); // Oscar-Meyer-Weiner
        ret = ProperSuffix(ret, "Mc"); // Scots
        ret = ProperSuffix(ret, "Mac"); // Scots

        // Special words:
        ret = SpecialWords(ret, "van"); // Dick van Dyke
        ret = SpecialWords(ret, "von"); // Baron von Bruin-Valt
        ret = SpecialWords(ret, "de");
        ret = SpecialWords(ret, "di");
        ret = SpecialWords(ret, "da"); // Leonardo da Vinci, Eduardo da Silva
        ret = SpecialWords(ret, "of"); // The Grand Old Duke of York
        ret = SpecialWords(ret, "the"); // William the Conqueror
        ret = SpecialWords(ret, "HRH"); // His/Her Royal Highness
        ret = SpecialWords(ret, "HRM"); // His/Her Royal Majesty
        ret = SpecialWords(ret, "H.R.H."); // His/Her Royal Highness
        ret = SpecialWords(ret, "H.R.M."); // His/Her Royal Majesty

        ret = ret.DealWithRomanNumerals(); // William Gates, III

        return ret;
    }

    public static string DealWithRomanNumerals(this string word) =>
        RomanNumeralsRegex.Replace(word, match => match.Value.ToUpperInvariant());

    private static string ProperSuffix(string word, string prefix)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        var lowerWord = word.ToLower();
        var lowerPrefix = prefix.ToLower();

        if (!lowerWord.Contains(lowerPrefix))
            return word;

        var index = lowerWord.IndexOf(lowerPrefix, StringComparison.Ordinal);

        // If the search string is at the end of the word ignore.
        if (index + prefix.Length == word.Length)
            return word;

#if NETSTANDARD
        return word.Substring(0, index) + prefix + CapitaliseFirstLetter(word.Substring(index + prefix.Length));
#else
        return string.Concat(word.AsSpan(0, index), prefix, CapitaliseFirstLetter(word[(index + prefix.Length)..]));
#endif
    }

    private static string SpecialWords(string word, string specialWord) =>
        word.Equals(specialWord, StringComparison.InvariantCultureIgnoreCase)
            ? specialWord
            : word;

    private static string CapitaliseFirstLetter(string word) =>
#if NETSTANDARD
        char.ToUpper(word[0]) + word.Substring(1).ToLower();
#else
        char.ToUpper(word[0]) + word[1..].ToLower();
#endif
}