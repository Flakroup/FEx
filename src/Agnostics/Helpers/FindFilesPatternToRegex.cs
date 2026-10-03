using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FEx.Agnostics.Helpers;

/// <summary>Emulates Windows file-search wildcard matching (<c>*</c> and <c>?</c>) by converting patterns to regular expressions.</summary>
public static class FindFilesPatternToRegex
{
    private const string NonDotCharacters = "[^.]*";
    private static Regex HasQuestionMarkRegEx { get; } = new(@"\?", RegexOptions.Compiled);

    private static Regex IllegalCharactersRegex { get; } = new("[" + @"\/:<>|*" + "\"]", RegexOptions.Compiled);

    private static Regex CatchExtentionRegex { get; } = new(@"^\s*.+\.([^\.]+)\s*$", RegexOptions.Compiled);

    /// <summary>Returns the first name matching the wildcard pattern.</summary>
    /// <param name="pattern">The wildcard pattern to match.</param>
    /// <param name="name">The name to test.</param>
    /// <returns>The name if it matches; otherwise <see langword="null"/>.</returns>
    public static string? FindFileEmulator(this string pattern, string name) =>
        pattern.FindFilesEmulator(name).FirstOrDefault();

    /// <summary>Returns the names that match the wildcard pattern.</summary>
    /// <param name="pattern">The wildcard pattern to match.</param>
    /// <param name="names">The names to test.</param>
    /// <returns>The names that match.</returns>
    public static IEnumerable<string> FindFilesEmulator(this string pattern, params string[] names) =>
        FindFilesEmulator(names, patterns: pattern);

    /// <summary>Returns the file if its name matches any of the wildcard patterns.</summary>
    /// <param name="file">The file to test.</param>
    /// <param name="patterns">The wildcard patterns to match against the file name.</param>
    /// <returns>The file if it matches; otherwise <see langword="null"/>.</returns>
    public static FileInfo? FindFileEmulator(this FileInfo file, params string[] patterns) =>
        FindFilesEmulator([file], patterns).FirstOrDefault();

    /// <summary>Returns the files whose names match any of the wildcard patterns.</summary>
    /// <param name="files">The files to filter.</param>
    /// <param name="patterns">The wildcard patterns to match against file names.</param>
    /// <returns>The files that match.</returns>
    public static IEnumerable<FileInfo> FindFilesEmulator(this IEnumerable<FileInfo> files, params string[] patterns) =>
        FindFilesEmulator(files, x => x.Name, patterns);

    /// <summary>Returns the items whose selected string matches any of the wildcard patterns.</summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <param name="items">The items to filter.</param>
    /// <param name="selector">Selects the string to test from each item; when <see langword="null"/>, string items are tested directly and other items never match.</param>
    /// <param name="patterns">The wildcard patterns to match.</param>
    /// <returns>The items that match.</returns>
    public static IEnumerable<T> FindFilesEmulator<T>(IEnumerable<T> items,
                                                      Func<T, string>? selector = null,
                                                      params string[] patterns)
    {
        var regexes = patterns.Select(Convert).ToArray();

        return items.Where(i =>
        {
            var str = i as string;

            return str is not null && selector is null
                ? regexes.Any(x => x.IsMatch(str))
                : selector is not null && regexes.Any(x => x.IsMatch(selector(i)));
        });
    }

    /// <summary>Converts a file-search wildcard pattern into a case-insensitive anchored regular expression.</summary>
    /// <param name="pattern">The wildcard pattern, where <c>*</c> matches any run of characters and <c>?</c> any single character.</param>
    /// <returns>A compiled regular expression equivalent to the pattern.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty or contains illegal path characters.</exception>
    public static Regex Convert(string pattern)
    {
        if (pattern is null)
            throw new ArgumentNullException(nameof(pattern), "Pattern is null");

        pattern = pattern.Trim();

        if (pattern.Length == 0)
            throw new ArgumentException("Pattern is empty.");

        if (pattern.PathHasIllegalCharacters())
            throw new ArgumentException("Pattern contains illegal characters.");

        var hasExtension = CatchExtentionRegex.IsMatch(pattern);
        var matchExact = false;

        if (HasQuestionMarkRegEx.IsMatch(pattern))
        {
            matchExact = true;
        }
        else if (hasExtension)
        {
            var match = CatchExtentionRegex.Match(pattern);

            if (match.Groups.Count > 1)
                matchExact = match.Groups[1].Length != 3;
        }

        var regexString = Regex.Escape(pattern);
        regexString = "^" + Regex.Replace(regexString, @"\\\*", ".*");
        regexString = Regex.Replace(regexString, @"\\\?", ".");

        if (!matchExact && hasExtension)
            regexString += NonDotCharacters;

        regexString += "$";

        return new(regexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    /// <summary>Determines whether the pattern contains any of the characters <c>\ / : &lt; &gt; | * "</c>.</summary>
    /// <param name="pattern">The text to inspect.</param>
    /// <returns><see langword="true"/> if an illegal character is present.</returns>
    public static bool PathHasIllegalCharacters(this string pattern) => IllegalCharactersRegex.IsMatch(pattern);
}