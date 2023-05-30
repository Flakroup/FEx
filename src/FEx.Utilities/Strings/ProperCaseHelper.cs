using System;
using System.Text.RegularExpressions;

namespace FEx.Utilities.Strings;

// http://stackoverflow.com/questions/32149/does-anyone-have-a-good-proper-case-algorithm
public static class ProperCaseHelper
{
    public static bool IsAllUpperOrAllLower(this string input) =>
        input.ToLower().Equals(input) || input.ToUpper().Equals(input);

    public static string WordToProperCase(string word)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        // Standard case
        string ret = CapitaliseFirstLetter(word);

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

        ret = DealWithRomanNumerals(ret); // William Gates, III

        return ret;
    }

    public static string DealWithRomanNumerals(this string word)
    {
        return new Regex(@"\b(?!Xi\b)(X|XX|XXX|XL|L|LX|LXX|LXXX|XC|C)?(I|II|III|IV|V|VI|VII|VIII|IX)?\b",
            RegexOptions.IgnoreCase).Replace(word, match => match.Value.ToUpperInvariant());
    }

    private static string ProperSuffix(string word, string prefix)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        string lowerWord = word.ToLower();
        string lowerPrefix = prefix.ToLower();

        if (!lowerWord.Contains(lowerPrefix))
            return word;

        int index = lowerWord.IndexOf(lowerPrefix, StringComparison.Ordinal);

        // If the search string is at the end of the word ignore.
        return index + prefix.Length == word.Length
            ? word
            : word.Substring(0, index) + prefix + CapitaliseFirstLetter(word.Substring(index + prefix.Length));
    }

    private static string SpecialWords(string word, string specialWord) =>
        word.Equals(specialWord, StringComparison.InvariantCultureIgnoreCase)
            ? specialWord
            : word;

    private static string CapitaliseFirstLetter(string word) =>
        char.ToUpper(word[0]) + word.Substring(1).ToLower();
}