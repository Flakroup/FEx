using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace FEx.Analyzers.Tests;

/// <summary>Every rule links to its section in the package readme, and the readme has a section for every rule.</summary>
public partial class RuleDocumentationTests
{
    private static readonly string ReadmePath = Path.Combine(RepositoryLocator.FindRoot(), "src", "Analyzers", "README.md");

    private static readonly Dictionary<string, DiagnosticDescriptor> Descriptors = typeof(NonAtomicCompoundOperatorAnalyzer).Assembly
        .GetExportedTypes()
        .Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
        .SelectMany(type => ((DiagnosticAnalyzer)Activator.CreateInstance(type)!).SupportedDiagnostics)
        .ToDictionary(descriptor => descriptor.Id);

    public static TheoryData<string> RuleIds => new(Descriptors.Keys.Order());

    // The commit the analyzer assembly was built from; this suite always builds in a git checkout, so there is one.
    private static readonly string BuildCommit = typeof(NonAtomicCompoundOperatorAnalyzer).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion
        .Split('+')[1];

    [Fact]
    public void The_links_pin_the_commit_the_package_was_built_from() =>
        BuildCommit.ShouldMatch("^[0-9a-f]{40}$", "a link to a moving branch breaks in an installed package once that branch drops the rule");

    [Theory]
    [InlineData("1.0.0+0123abc", "0123abc")]
    [InlineData("1.0.0", "develop")]
    [InlineData(null, "develop")]
    public void The_readme_ref_is_the_build_commit_or_develop(string? informationalVersion, string expectedRef) =>
        RuleDocumentation.ReadmeAt(informationalVersion).ShouldBe($"https://github.com/Flakroup/FEx/blob/{expectedRef}/src/Analyzers/README.md");

    [Fact]
    public void Every_FEX_rule_is_exported() =>
        Descriptors.Count.ShouldBe(58);

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void The_rule_links_to_a_readme_section_that_describes_it(string id)
    {
        var descriptor = Descriptors[id];
        descriptor.HelpLinkUri.ShouldNotBeNullOrWhiteSpace($"{id} has no helpLinkUri");
        var parts = descriptor.HelpLinkUri.Split('#');

        parts[0].ShouldBe($"https://github.com/Flakroup/FEx/blob/{BuildCommit}/src/Analyzers/README.md");
        parts.Length.ShouldBe(2);
        var sections = Sections();
        sections.ShouldContainKey(parts[1], $"{id} points at #{parts[1]}, which README.md has no heading for");
        sections[parts[1]].ShouldContain(descriptor.Title.ToString());
    }

    [Fact]
    public void The_readme_has_no_section_for_a_rule_that_does_not_exist() =>
        Sections().Keys
            .Order()
            .ShouldBe(Descriptors.Keys.Select(id => id.ToLowerInvariant()).Order());

    // GitHub anchors a heading by lower-casing it; the rule headings hold only the id, so that is the whole rule.
    private static Dictionary<string, string> Sections()
    {
        var sections = new Dictionary<string, string>();
        string? anchor = null;
        foreach (var line in File.ReadAllLines(ReadmePath))
        {
            var heading = HeadingPattern().Match(line);
            if (heading.Success)
            {
                anchor = heading.Groups[1].Value.ToLowerInvariant();
                sections[anchor] = string.Empty;
            }
            else if (anchor is not null)
            {
                sections[anchor] += line + "\n";
            }
        }

        return sections;
    }

    [GeneratedRegex(@"^#{1,6} (FEX\d{4})\s*$")]
    private static partial Regex HeadingPattern();
}
