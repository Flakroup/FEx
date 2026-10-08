using FEx.MSBuildx.Extensions;
using Microsoft.Build.Construction;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace FEx.MSBuildx.Tests;

public sealed class MSBuildExtensionsTests : IDisposable
{
    private readonly MSBuildTestWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public void FromFile_LoadsTheProjectWithTheGivenType()
    {
        var path = _workspace.WriteProject("Demo.csproj");

        var project = MSBuildExtensions.FromFile(path, SolutionProjectType.WebProject);
        _workspace.Track(project);

        project.Name.ShouldBe("Demo");
        project.ProjectType.ShouldBe(SolutionProjectType.WebProject);
        project.FullPath.ShouldBe(path);
    }

    [Fact]
    public void FromFile_ReusesAProjectThatIsAlreadyLoaded()
    {
        var path = _workspace.WriteProject("Demo.csproj");

        var first = _workspace.LoadFromFile(path);
        var second = _workspace.LoadFromFile(path);

        second.Project.ShouldBeSameAs(first.Project);
        second.ShouldNotBeSameAs(first);
    }

    [Fact]
    public void FromFile_PassesTheSolutionPackagesDirectoryOn()
    {
        var path = _workspace.WriteProject("Demo.csproj", items: """
            <Content Include="solpkgs_lib.dll" />
            <Content Include="local.dll" />
            """);

        _workspace.LoadFromFile(path, "solpkgs_").IncludedFiles.ShouldHaveSingleItem().EvaluatedInclude.ShouldBe("local.dll");
    }

    [Fact]
    public void FromFile_ForAProjectInASolution_UsesItsAbsolutePathAndType()
    {
        var solutionPath = _workspace.WriteSolution("Demo.sln", ("Demo", Path.Combine("Demo", "Demo.csproj")));
        _workspace.WriteProject(Path.Combine("Demo", "Demo.csproj"));
        var projectInSolution = SolutionFile.Parse(solutionPath).ProjectsInOrder.Single(x => x.ProjectType != SolutionProjectType.SolutionFolder);

        var project = projectInSolution.FromFile();
        _workspace.Track(project);

        project.Name.ShouldBe("Demo");
        project.ProjectType.ShouldBe(projectInSolution.ProjectType);
        project.FullPath.ShouldBe(projectInSolution.AbsolutePath);
    }
}
