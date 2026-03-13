using FEx.Agnostics.Extensions;
using FEx.Core.Collections.Concurrent;
using FEx.NuGetx;
using NuGet.Packaging.Core;
using System;
using System.Linq;

namespace FEx.MSBuildx;

public class MSProjectNuGetInstallation : NuGetPackageInstallation
{
    private string _projects;

    public ConcurrentObservableList<MSProject> MSProjects { get; }

    public string Projects
    {
        get => _projects;
        private set => SetProperty(ref _projects, value);
    }

    public MSProjectNuGetInstallation(PackageIdentity package)
        : base(package)
    {
        MSProjects = [];

        MSProjects.CollectionChangedObservable.Subscribe(_ =>
            Projects = string.Join(", ", MSProjects.Select(x => x.Name).OrderAlphanumBy(x => x)));
    }

    public void AddProject(MSProject project)
    {
        if (MSProjects.All(x => x.Name != project.Name))
            MSProjects.Add(project);
    }
}
