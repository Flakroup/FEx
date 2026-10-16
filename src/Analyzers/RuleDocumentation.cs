namespace FEx.Analyzers;

/// <summary>Where the rules are documented: one section per rule id in the package readme.</summary>
internal static class RuleDocumentation
{
    public const string ReadmeUrl = "https://github.com/Flakroup/FEx/blob/develop/src/Analyzers/README.md";

    /// <summary>The readme section of a rule, spelled the way GitHub anchors a heading that holds only the id.</summary>
    public static string HelpLink(string diagnosticId) => $"{ReadmeUrl}#{diagnosticId.ToLowerInvariant()}";
}
