using NuGet.Packaging.Core;
using NuGet.Versioning;
using Shouldly;
using System;
using System.Linq;
using Xunit;

namespace FEx.MSBuildx.Tests;

[Collection(InlineDispatcherScope.CollectionName)]
public sealed class MSProjectNuGetInstallationTests : IDisposable
{
    private readonly InlineDispatcherScope _dispatcher = new();
    private readonly MSBuildTestWorkspace _workspace = new();

    public void Dispose()
    {
        _workspace.Dispose();
        _dispatcher.Dispose();
    }

    [Fact]
    public void Constructor_DescribesThePackageAndStartsWithoutProjects()
    {
        using MSProjectNuGetInstallation installation = new(new("Pkg", NuGetVersion.Parse("1.2.3")));

        installation.Name.ShouldBe("Pkg");
        installation.Version.ShouldBe("1.2.3");
        installation.MSProjects.ShouldBeEmpty();
        installation.Projects.ShouldBeNull();
    }

    [Fact]
    public void AddProject_AddsTheProjectAndListsItsName()
    {
        using var installation = CreateInstallation();
        var project = CreateProject("Alpha");

        installation.AddProject(project);

        installation.MSProjects.ShouldHaveSingleItem().ShouldBeSameAs(project);
        installation.Projects.ShouldBe("Alpha");
    }

    [Fact]
    public void AddProject_ListsTheProjectNamesInAlphanumericOrder()
    {
        using var installation = CreateInstallation();

        installation.AddProject(CreateProject("Project10"));
        installation.AddProject(CreateProject("Project2"));
        installation.AddProject(CreateProject("Alpha"));

        installation.Projects.ShouldBe("Alpha, Project2, Project10");
    }

    [Fact]
    public void AddProject_IgnoresAProjectWhoseNameIsAlreadyListed()
    {
        using var installation = CreateInstallation();
        var first = CreateProject("Alpha");

        installation.AddProject(first);
        installation.AddProject(CreateProject("Alpha", "other"));
        installation.AddProject(first);

        installation.MSProjects.Select(x => x.Name).ShouldBe(["Alpha"]);
    }

    [Fact]
    public void Dispose_StopsTheProjectListFromUpdating()
    {
        var installation = CreateInstallation();
        var projects = installation.MSProjects;
        var listedNames = () => installation.Projects;
        installation.AddProject(CreateProject("Alpha"));

        installation.Dispose();
        projects.Add(CreateProject("Beta"));

        projects.Count.ShouldBe(2);
        listedNames().ShouldBe("Alpha");
    }

    [Fact]
    public void Dispose_CanBeCalledTwice()
    {
        var installation = CreateInstallation();

        Should.NotThrow(() =>
        {
            installation.Dispose();
            installation.Dispose();
        });
    }

    private static MSProjectNuGetInstallation CreateInstallation() => new(new PackageIdentity("Pkg", NuGetVersion.Parse("1.0.0")));

    private MSProject CreateProject(string name, string folder = "") =>
        _workspace.Load(_workspace.WriteProject(System.IO.Path.Combine(folder, name + ".csproj")));
}
