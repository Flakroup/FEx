using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Shouldly;
using Xunit;

namespace FEx.Analyzers.Tests;

/// <summary>
/// FEx runs its own analyzer from source, through a <c>ProjectReference</c> in the root <c>Directory.Build.targets</c>.
/// A build stays green when that reference stops loading the analyzer (no <c>OutputItemType="Analyzer"</c>, a mistyped path)
/// and a pack stays green until it runs after the merge when the reference stops being build-only (no
/// <c>ReferenceOutputAssembly="false"</c>, which is what keeps a FEx.Analyzers dependency out of every nuspec), so the
/// wiring is pinned here.
/// </summary>
public partial class OwnAnalyzerWiringTests
{
    private static readonly string Root = RepositoryLocator.FindRoot();

    private static readonly XElement ItemGroup = XDocument.Load(Path.Combine(Root, "Directory.Build.targets"))
        .Descendants("ItemGroup")
        .Single(group => group.Elements("ProjectReference").Any());

    private static XElement Reference => ItemGroup.Elements("ProjectReference").Single();

    [Fact]
    public void The_reference_points_at_the_analyzer_project_that_exists()
    {
        var include = Reference.Attribute("Include")!.Value;

        include.ShouldStartWith("$(MSBuildThisFileDirectory)");
        var path = Path.GetFullPath(Path.Combine(Root, include["$(MSBuildThisFileDirectory)".Length..]));
        path.ShouldBe(Path.Combine(Root, "src", "Analyzers", "FEx.Analyzers.csproj"));
        File.Exists(path).ShouldBeTrue();
    }

    [Fact]
    public void The_reference_loads_the_project_as_an_analyzer() =>
        Reference.Attribute("OutputItemType")!.Value.ShouldBe("Analyzer");

    [Fact]
    public void The_reference_stays_out_of_the_compilation_and_the_nuspec() =>
        Reference.Attribute("ReferenceOutputAssembly")!.Value.ShouldBe("false");

    [Fact]
    public void The_reference_goes_to_every_project_except_the_analyzer_and_its_tests()
    {
        var condition = ItemGroup.Attribute("Condition")!.Value;

        ExcludedProject().Matches(condition).Select(match => match.Groups["name"].Value)
            .ShouldBe(["FEx.Analyzers", "FEx.Analyzers.Tests"]);
        // Only "!= ... And !=" clauses: an Or or an equality test would widen or invert the exclusion.
        ExcludedProject().Replace(condition, "").Trim().ShouldBe("And");
    }

    [Fact]
    public void The_published_package_is_switched_off_so_its_older_rules_do_not_run_beside_the_project()
    {
        var props = XDocument.Load(Path.Combine(Root, "Directory.Build.props"));

        props.Descendants("DevConfigsFExAnalyzers").Select(element => element.Value).ShouldBe(["false"]);
    }

    [GeneratedRegex(@"'\$\(MSBuildProjectName\)'\s*!=\s*'(?<name>[^']+)'")]
    private static partial Regex ExcludedProject();
}
