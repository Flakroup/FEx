using System;
using System.Text;
using System.Text.RegularExpressions;

namespace FEx.Flurlx.Services;

/// <summary>
/// Masks the credential-bearing parts of URLs (userinfo, query string values, fragment) before they reach a log sink.
/// </summary>
/// <remarks>
/// Flurl (and <see cref="System.Net.Http.HttpRequestException" />) embed the full request URL in exception messages, so
/// an API key or token passed in the query string or as userinfo would otherwise be written to the logs on every
/// retry/fallback. Scheme, host, port, path and query parameter names are kept for diagnostics; userinfo, every query
/// value (and every valueless query token) and the fragment are masked.
/// Path segments are NOT redacted: APIs that carry a secret in the path (e.g. bot tokens, webhook URLs) are not covered.
/// Once a query or fragment starts, the rest of the line is treated as part of it (query text may contain unencoded
/// quotes or spaces), so trailing text after such a URL is masked as well.
/// </remarks>
internal static class UrlLogRedactor
{
    internal const string Mask = "***";

    // Linear pattern: starts only at a token boundary; the authority/path runs to whitespace, and a query or fragment
    // runs to the end of the line because it may hold unencoded quotes or spaces (e.g. OData "$filter=Name eq 'bob'").
    private static readonly Regex AbsoluteUrlRegex = new(@"(?<![A-Za-z0-9+.\-])[A-Za-z][A-Za-z0-9+.\-]*://[^\s?#]*(?:[?#][^\r\n]*)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Describes an exception for logging with every URL in its message redacted.
    /// </summary>
    internal static string DescribeException(Exception? exception) =>
        exception is null
            ? "Unknown error"
            : RedactUrls(exception.Message);

    /// <summary>
    /// Redacts every absolute URL found in free text (e.g. an exception message).
    /// </summary>
    internal static string RedactUrls(string? text) =>
        string.IsNullOrEmpty(text)
            ? string.Empty
            : AbsoluteUrlRegex.Replace(text!, static m => RedactUrl(m.Value));

    /// <summary>
    /// Redacts a single absolute URL: userinfo, query values and fragment are masked.
    /// </summary>
    internal static string RedactUrl(string url)
    {
        var schemeEnd = url.IndexOf("://", StringComparison.Ordinal);

        if (schemeEnd < 0)
            return url;

        var authorityStart = schemeEnd + 3;
        var authorityEnd = IndexOfAny(url, authorityStart, '/', '?', '#');
        var sb = new StringBuilder(url.Length);
        sb.Append(url, 0, authorityStart);

        var authority = url.Substring(authorityStart, authorityEnd - authorityStart);
        var at = authority.LastIndexOf('@');
        sb.Append(at < 0 ? authority : Mask + authority.Substring(at));

        var queryStart = url.IndexOf('?', authorityEnd);
        var fragmentStart = url.IndexOf('#', authorityEnd);

        if (fragmentStart >= 0 && queryStart > fragmentStart)
            queryStart = -1; // '?' inside the fragment is not a query

        var pathEnd = queryStart >= 0 ? queryStart
            : fragmentStart >= 0 ? fragmentStart
            : url.Length;

        sb.Append(url, authorityEnd, pathEnd - authorityEnd);

        if (queryStart >= 0)
        {
            var queryEnd = fragmentStart >= 0 ? fragmentStart : url.Length;
            var parameters = url.Substring(queryStart + 1, queryEnd - queryStart - 1).Split('&');
            sb.Append('?');

            for (var i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                    sb.Append('&');

                var parameter = parameters[i];

                if (parameter.Length == 0)
                    continue;

                // A valueless token ("?<apikey>") may itself be the secret, so it is masked as a whole.
                var eq = parameter.IndexOf('=');
                sb.Append(eq < 0 ? Mask : parameter.Substring(0, eq + 1) + Mask);
            }
        }

        if (fragmentStart >= 0)
            sb.Append('#').Append(Mask);

        return sb.ToString();
    }

    private static int IndexOfAny(string s, int start, params char[] chars)
    {
        var index = s.IndexOfAny(chars, start);

        return index < 0 ? s.Length : index;
    }
}
