using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Collections.Concurrent;
using FEx.Asyncx.Abstractions;
using FEx.Core.Collections.Concurrent;
using FEx.MSBuildx.Extensions;
using Microsoft.Build.Construction;
using NuGet.Packaging.Core;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.MSBuildx;

public class MSSolution : AsyncInitializable
{
    public string SolutionDir { get; }
    public IReadOnlyList<MSProject> Projects { get; }
    public SolutionFile Solution { get; }
    public string SolutionFilePath { get; }
    public string Name { get; }
    public string SolutionPackagesDir { get; }
    public ConcurrentDictionary<PackageIdentity, HashSet<string>> NuGetPackages { get; }
    public ConcurrentObservableList<MSProjectNuGetInstallation> InstalledNuGetPackages { get; }
    protected ConcurrentList<MSProject> ProjectsList => (ConcurrentList<MSProject>)Projects;

    public MSSolution(string solutionFile)
    {
        SolutionFilePath = solutionFile;
        SolutionDir = Path.GetDirectoryName(solutionFile);
        Solution = SolutionFile.Parse(solutionFile);
        Name = Path.GetFileNameWithoutExtension(SolutionFilePath);
        SolutionPackagesDir = Path.Combine(SolutionDir ?? string.Empty, "packages");
        NuGetPackages = new();
        InstalledNuGetPackages = [];
        Projects = new ConcurrentList<MSProject>();

        BeginInitialization();
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();
        await GetProjectsListAsync();
    }

    private async Task GetProjectsListAsync() =>
        await Solution.ProjectsInOrder.Where(x => x.ProjectType != SolutionProjectType.SolutionFolder)
            .WithWhenAllAsync(AddProject);

    private void AddProject(ProjectInSolution project)
    {
        MSProject msProj = project.FromFile(SolutionPackagesDir);

        foreach (PackageIdentity pkg in msProj.NuGetPackages)
        {
            NuGetPackages.AddOrUpdate(pkg,
                _ =>
                [
                    msProj.Name
                ],
                (_, set) =>
                {
                    set.Add(msProj.Name);

                    return set;
                });

            InstalledNuGetPackages.Write(() =>
            {
                MSProjectNuGetInstallation nuGet = InstalledNuGetPackages.FirstOrDefault(x => x.Name == pkg.Id);

                if (nuGet is null)
                {
                    nuGet = new(pkg);
                    InstalledNuGetPackages.Add(nuGet);
                }

                nuGet.AddProject(msProj);
            });
        }

        ProjectsList.Add(msProj);
    }
}
