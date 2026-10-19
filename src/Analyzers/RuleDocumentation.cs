using System.Reflection;

namespace FEx.Analyzers;

/// <summary>Where the rules are documented: one section per rule id in the package readme.</summary>
internal static class RuleDocumentation
{
    /// <summary>
    /// The readme at the commit this assembly was built from, so an installed package keeps linking to the text that
    /// describes its own rules after develop renames or drops one. The SDK appends that commit to the informational
    /// version (<c>1.0.0+&lt;sha&gt;</c>); a build without git records none and links to develop.
    /// </summary>
    public static string ReadmeUrl { get; } = ReadmeAt(typeof(RuleDocumentation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>The readme section of a rule, spelled the way GitHub anchors a heading that holds only the id.</summary>
    public static string HelpLink(string diagnosticId) => $"{ReadmeUrl}#{diagnosticId.ToLowerInvariant()}";

    private static string ReadmeAt(string? informationalVersion)
    {
        var plus = informationalVersion?.IndexOf('+') ?? -1;
        var commit = plus < 0 ? "develop" : informationalVersion!.Substring(plus + 1);

        return $"https://github.com/Flakroup/FEx/blob/{commit}/src/Analyzers/README.md";
    }
}
