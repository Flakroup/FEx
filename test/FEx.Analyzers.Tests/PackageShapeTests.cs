using System.IO;
using System.Linq;
using System.Xml.Linq;
using Shouldly;
using Xunit;

namespace FEx.Analyzers.Tests;

/// <summary>
/// The package is analyzer-only: the dll must sit in <c>analyzers/dotnet/cs</c> and nowhere else, and the package must
/// not flow to consumers as a dependency. Pack first runs in the CI build of <c>develop</c>, so the project file is the
/// place to pin the layout before then.
/// </summary>
public class PackageShapeTests
{
    private static readonly XDocument Project = XDocument.Load(Path.Combine(RepositoryLocator.FindRoot(), "src", "Analyzers", "FEx.Analyzers.csproj"));

    [Theory]
    [InlineData("IncludeBuildOutput", "false")]
    [InlineData("DevelopmentDependency", "true")]
    [InlineData("SuppressDependenciesWhenPacking", "true")]
    public void The_project_declares_an_analyzer_only_package(string property, string value) =>
        Project.Descendants(property).Select(element => element.Value).ShouldBe([value]);

    [Fact]
    public void The_project_packs_the_built_dll_into_the_analyzers_folder_only()
    {
        var packed = Project.Descendants("None").Where(item => (string?)item.Attribute("Pack") == "true").ToList();

        packed.ShouldHaveSingleItem();
        packed[0].Attribute("Include")!.Value.ShouldEndWith("$(AssemblyName).dll");
        packed[0].Attribute("PackagePath")!.Value.ShouldBe("analyzers/dotnet/cs");
    }
}
