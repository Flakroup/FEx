using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FEx.MSBuildx.Tests;

[Collection(InlineDispatcherScope.CollectionName)]
public sealed class MSSolutionTests : IDisposable
{
    private readonly InlineDispatcherScope _dispatcher = new();
    private readonly MSBuildTestWorkspace _workspace = new();

    private MSSolution? _solution;

    private MSSolution Solution => _solution!;

    public void Dispose()
    {
        if (_solution is not null)
            MSBuildTestWorkspace.UnloadProjects(_solution.Projects);

        _workspace.Dispose();
        _dispatcher.Dispose();
    }

    [Fact]
    public async Task Constructor_ReadsTheSolutionIdentity()
    {
        await LoadAsync();

        Solution.SolutionFilePath.ShouldBe(Path.Combine(_workspace.Dir, "Demo.sln"));
        Solution.SolutionDir.ShouldBe(_workspace.Dir);
        Solution.Name.ShouldBe("Demo");
        Solution.SolutionPackagesDir.ShouldBe(Path.Combine(_workspace.Dir, "packages"));
        Solution.Solution.ProjectsInOrder.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Projects_ListsEveryProjectExceptSolutionFolders()
    {
        await LoadAsync();

        Solution.Projects.Select(x => x.Name).ShouldBe(["Alpha", "Beta"], true);
    }

    [Fact]
    public async Task NuGetPackages_MapsEachPackageToTheProjectsThatUseIt()
    {
        await LoadAsync();

        var byPackage = Solution.NuGetPackages.ToDictionary(x => $"{x.Key.Id} {x.Key.Version}", x => x.Value.OrderBy(n => n).ToArray());

        byPackage.Keys.OrderBy(x => x).ShouldBe(["Only 2.0.0", "Shared 1.0.0"]);
        byPackage["Shared 1.0.0"].ShouldBe(["Alpha", "Beta"]);
        byPackage["Only 2.0.0"].ShouldBe(["Beta"]);
    }

    [Fact]
    public async Task InstalledNuGetPackages_GroupsTheProjectsPerPackage()
    {
        await LoadAsync();

        var installations = Solution.InstalledNuGetPackages.OrderBy(x => x.Name).ToArray();

        installations.Select(x => x.Name).ShouldBe(["Only", "Shared"]);
        installations[0].MSProjects.Select(x => x.Name).ShouldBe(["Beta"]);
        installations[1].MSProjects.Select(x => x.Name).OrderBy(x => x).ShouldBe(["Alpha", "Beta"]);
        installations[1].Version.ShouldBe("1.0.0");
    }

    [Fact]
    public async Task Projects_AreEvaluatedAgainstTheSolutionPackagesDirectory()
    {
        await LoadAsync();

        var alpha = Solution.Projects.Single(x => x.Name == "Alpha");

        alpha.IncludedFiles.Select(x => x.EvaluatedInclude).ShouldBe(["alpha.txt"]);
    }

    [Fact]
    public async Task Constructor_YieldsNoProjects_ForAnEmptySolution()
    {
        await LoadAsync(WriteSolution());

        Solution.Projects.ShouldBeEmpty();
        Solution.NuGetPackages.ShouldBeEmpty();
        Solution.InstalledNuGetPackages.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_Throws_WhenTheSolutionFileDoesNotExist() =>
        Should.Throw<Exception>(() =>
        {
            using MSSolution solution = new(Path.Combine(_workspace.Dir, "missing.sln"));
        });

    private string WriteSolution(params (string Name, string RelativePath)[] projects) =>
        _workspace.WriteSolution("Demo.sln", projects);

    private async Task LoadAsync(string? solutionPath = null)
    {
        if (solutionPath is null)
        {
            _workspace.WriteProject(Path.Combine("Alpha", "Alpha.csproj"),
                items: $"""
                       <PackageReference Include="Shared" Version="1.0.0" />
                       <None Include="alpha.txt" />
                       <None Include="{Path.Combine(_workspace.Dir, "packages", "lib.dll")}" />
                       """);

            _workspace.WriteProject(Path.Combine("Beta", "Beta.csproj"),
                items: """
                       <PackageReference Include="Shared" Version="1.0.0" />
                       <PackageReference Include="Only" Version="2.0.0" />
                       """);

            solutionPath = WriteSolution(("Alpha", Path.Combine("Alpha", "Alpha.csproj")),
                ("Beta", Path.Combine("Beta", "Beta.csproj")));
        }

        _solution = new(solutionPath);
        await _solution.InitializeAsync();
    }
}
