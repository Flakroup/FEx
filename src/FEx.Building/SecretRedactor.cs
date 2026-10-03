using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FEx.Building;

/// <summary>
/// The one place that decides what a build log may not show. Every line this build library prints passes
/// through it - <see cref="FExBuild" /> routes the whole Serilog pipeline through a
/// <see cref="RedactingLogSink" /> before the first line is written - so a new log call cannot forget it.
/// </summary>
/// <remarks>
/// Three independent passes, because no single one is enough:
/// <list type="bullet">
/// <item><b>By option name.</b> The token after any spelling of a secret option is replaced, whatever its value.
/// NUKE resolves a repeated option to its LAST occurrence, so a value-only pass never learns the earlier ones
/// - <c>--nuget-api-key A --nuget-api-key B</c> printed <c>A</c> in full (#55). Covers a getter that throws on
/// a value it was given, too: that value is never handed over, so it cannot be matched by value.</item>
/// <item><b>By value.</b> A secret that arrived through an environment variable or a parameters file has no
/// option on the command line, but its value can still turn up in any line.</item>
/// <item><b>URL credentials.</b> A feed URL can carry its own credential - <c>https://user:TOKEN@host/...</c>
/// is the documented form for GitHub Packages, Azure Artifacts and MyGet - so the user-info of every URL and
/// the value of every credential-like query parameter is replaced, whichever parameter or source the URL came
/// from (#57).</item>
/// </list>
/// </remarks>
public sealed class SecretRedactor
{
    /// <summary>What a secret is replaced by everywhere this build logs.</summary>
    public const string Mask = "***";

    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    // Only inside a URL: scheme://authority[/path][?query][#fragment]. A quote is legal in user-info and in a
    // query value, so it does not end the match - over-redacting a trailing quote is harmless, stopping early is not.
    private static readonly Regex Url = new(@"\b(?<scheme>[a-z][a-z0-9+.\-]*)://(?<rest>[^\s""<>]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        MatchTimeout);

    // The WHOLE parameter name, or its last segment after - _ or . (x-api-key, client_secret) - never a substring,
    // so author=, design= and assign= stay readable.
    private static readonly Regex CredentialQueryName =
        new(@"^(?:.*[-_.])?(?:api[-_]?key|key|token|access[-_]?token|auth|password|passwd|pwd|secret|sig|signature|credentials?)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            MatchTimeout);

    private readonly Regex? _secretOptions;
    private readonly string[] _secretValues;

    /// <param name="secretOptionNames">
    /// Parameter names whose option VALUES are always redacted, e.g. <c>NuGetApiKey</c>. Every spelling NUKE
    /// accepts for it matches: <c>--nuget-api-key</c>, <c>-NuGetApiKey</c>, any casing, with <c>=</c> or <c>:</c>.
    /// </param>
    /// <param name="secretValues">Resolved secret values, redacted wherever they appear. Empty ones are ignored.</param>
    public SecretRedactor(IEnumerable<string> secretOptionNames, IEnumerable<string> secretValues)
    {
        var names = secretOptionNames.Where(static n => !string.IsNullOrWhiteSpace(n))
            .Select(OptionNamePattern)
            .Distinct()
            .ToList();

        // An option token, then either "=value"/":value" or every following token up to the next option -
        // NUKE binds all of those to the option (array parameters), so all of them are its value.
        _secretOptions = names.Count == 0
            ? null
            : new(@$"(?<![^\s""'])(?<option>(?:-+|/)(?:{string.Join("|", names)}))"
                  + @"(?:(?<separator>[=:])(?:""[^""]*""|'[^']*'|\S+)|(?:\s+(?!-)(?:""[^""]*""|'[^']*'|\S+))+)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                MatchTimeout);

        // Longest first, so a secret that contains another is not left half-masked.
        _secretValues =
        [
            .. secretValues.Where(static v => !string.IsNullOrEmpty(v))
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(static v => v.Length)
        ];
    }

    /// <summary>Returns <paramref name="text" /> with every secret it carries replaced by <see cref="Mask" />.</summary>
    public string Redact(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        if (_secretOptions is not null)
            text = _secretOptions.Replace(text,
                static m => m.Groups["separator"].Success
                    ? $"{m.Groups["option"].Value}{m.Groups["separator"].Value}{Mask}"
                    : $"{m.Groups["option"].Value} {Mask}");

        foreach (var value in _secretValues)
            text = text.Replace(value, Mask);

        return Url.Replace(text, static m => $"{m.Groups["scheme"].Value}://{RedactUrl(m.Groups["scheme"].Value, m.Groups["rest"].Value)}");
    }

    // scheme://user[:password]@host?name=value - the user-info and every credential-named query value go, the
    // scheme, host and path stay so the line still says where. A bare SSH user (ssh://git@host) is an account
    // name, not a credential, and a git remote diagnostic needs it; a user WITH a password is masked either way.
    private static string RedactUrl(string scheme, string rest)
    {
        var authorityEnd = rest.IndexOfAny(['/', '?', '#']);
        var authority = authorityEnd < 0 ? rest : rest[..authorityEnd];
        var tail = authorityEnd < 0 ? string.Empty : rest[authorityEnd..];
        var at = authority.LastIndexOf('@');

        if (at >= 0)
        {
            var userInfo = authority[..at];
            var bareSshUser = scheme.EndsWith("ssh", StringComparison.OrdinalIgnoreCase) && userInfo.IndexOf(':') < 0;

            if (!bareSshUser)
                authority = $"{Mask}{authority[at..]}";
        }

        var queryStart = tail.IndexOf('?');

        if (queryStart < 0)
            return authority + tail;

        var fragmentStart = tail.IndexOf('#', queryStart);
        var queryEnd = fragmentStart < 0 ? tail.Length : fragmentStart;

        var query = string.Join("&",
            tail[(queryStart + 1)..queryEnd]
                .Split('&')
                .Select(static pair =>
                {
                    var eq = pair.IndexOf('=');

                    return eq > 0 && CredentialQueryName.IsMatch(pair[..eq])
                        ? $"{pair[..(eq + 1)]}{Mask}"
                        : pair;
                }));

        return $"{authority}{tail[..(queryStart + 1)]}{query}{tail[queryEnd..]}";
    }

    // NuGetApiKey -> N-*u-*G-*e-*t-*A-*p-*i-*K-*e-*y: NUKE compares option names with every dash removed and
    // case ignored, so --nuget-api-key, --nugetapikey and -NuGetApiKey all bind to the same parameter.
    private static string OptionNamePattern(string name) =>
        string.Join("-*",
            name.Where(static c => c is not '-' and not '_')
                .Select(static c => Regex.Escape(c.ToString())));
}
