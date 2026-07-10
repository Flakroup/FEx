using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System.IO;
using System.Linq;

namespace FEx.MSBuildx.Extensions;

public static class MSBuildExtensions
{
    public static MSProject FromFile(this ProjectInSolution projectInSolution, string? solutionPackagesDir = null) =>
        FromFile(projectInSolution.AbsolutePath, projectInSolution.ProjectType, solutionPackagesDir);

    public static MSProject FromFile(string projectFilePath,
                                     SolutionProjectType projectType,
                                     string? solutionPackagesDir = null)
    {
        projectFilePath = Path.GetFullPath(projectFilePath);

        var loadedProjects = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(projectFilePath);

        var project = loadedProjects.Count > 0
            ? loadedProjects.First()
            : Project.FromFile(projectFilePath, new());

        return new(project, projectType, solutionPackagesDir);
    }
}