using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FEx.Agnostics.Helpers;

public static class FindFilesPatternToRegex
{
    private const string NonDotCharacters = "[^.]*";
    private static Regex HasQuestionMarkRegEx { get; } = new(@"\?", RegexOptions.Compiled);

    private static Regex IllegalCharactersRegex { get; } = new("[" + @"\/:<>|*" + "\"]", RegexOptions.Compiled);

    private static Regex CatchExtentionRegex { get; } = new(@"^\s*.+\.([^\.]+)\s*$", RegexOptions.Compiled);

    public static string? FindFileEmulator(this string pattern, string name) =>
        pattern.FindFilesEmulator(name).FirstOrDefault();

    public static IEnumerable<string> FindFilesEmulator(this string pattern, params string[] names) =>
        FindFilesEmulator(names, patterns: pattern);

    public static FileInfo? FindFileEmulator(this FileInfo file, params string[] patterns) =>
        FindFilesEmulator([file], patterns).FirstOrDefault();

    public static IEnumerable<FileInfo> FindFilesEmulator(this IEnumerable<FileInfo> files, params string[] patterns) =>
        FindFilesEmulator(files, x => x.Name, patterns);

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

    public static bool PathHasIllegalCharacters(this string pattern) => IllegalCharactersRegex.IsMatch(pattern);
}