using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using FEx.MSBuildx.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FEx.MSBuildx.Tests;

/// <summary>
/// A throw-away directory that holds a project (and its files) to evaluate with Microsoft.Build. The projects declare
/// every property themselves and import nothing, so evaluation needs neither an SDK nor Visual Studio.
/// </summary>
internal sealed class MSBuildTestWorkspace : IDisposable
{
    private const string Sep = "$([System.IO.Path]::DirectorySeparatorChar)";

    private static readonly object ToolsetLock = new();

    private readonly List<MSProject> _loaded = [];

    public string Dir { get; } = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "fex-msbuildx-" + Guid.NewGuid().ToString("N"))).FullName;

    public MSBuildTestWorkspace() => EnsureToolset();

    /// <summary>Builds the property group shared by the test projects; a null argument omits that property.</summary>
    public static string Props(string? targetFramework = "net10.0",
                               string? outDir = "bin" + Sep + "Debug" + Sep,
                               string extra = "")
    {
        var props = "<AssemblyName>$(MSBuildProjectName)</AssemblyName>"
                    + "<Configuration>Debug</Configuration>"
                    + "<ProjectDir>$(MSBuildProjectDirectory)" + Sep + "</ProjectDir>";

        if (targetFramework is not null)
            props += $"<TargetFramework>{targetFramework}</TargetFramework>";

        if (outDir is not null)
            props += $"<OutDir>{outDir}</OutDir>";

        return props + extra;
    }

    public string WriteFile(string relativePath, string content = "")
    {
        var path = Path.Combine(Dir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);

        return path;
    }

    public string WriteProject(string relativePath, string? properties = null, string items = "")
    {
        var xml = $"""
                   <Project>
                     <PropertyGroup>{properties ?? Props()}</PropertyGroup>
                     <ItemGroup>{items}</ItemGroup>
                   </Project>
                   """;

        return WriteFile(relativePath, xml);
    }

    public MSProject Load(string projectPath,
                          string? solutionPackagesDir = null,
                          bool is64Bit = false)
    {
        var project = new MSProject(Project.FromFile(projectPath, new()),
            SolutionProjectType.KnownToBeMSBuildFormat,
            solutionPackagesDir,
            is64Bit);

        _loaded.Add(project);

        return project;
    }

    /// <summary>Writes a classic <c>.sln</c> that lists the given projects plus one solution folder.</summary>
    public string WriteSolution(string relativePath, params (string Name, string RelativePath)[] projects)
    {
        var text = new StringBuilder()
            .AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00")
            .AppendLine("# Visual Studio Version 17");

        for (var i = 0; i < projects.Length; i++)
        {
            text.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projects[i].Name}\", \"{projects[i].RelativePath.Replace('/', '\\')}\", \"{{00000000-0000-0000-0000-00000000000{i + 1}}}\"")
                .AppendLine("EndProject");
        }

        text.AppendLine("Project(\"{2150E333-8FDC-42A3-9474-1A3956D46DE8}\") = \"Items\", \"Items\", \"{00000000-0000-0000-0000-0000000000FF}\"")
            .AppendLine("EndProject")
            .AppendLine("Global")
            .AppendLine("EndGlobal");

        return WriteFile(relativePath, text.ToString());
    }

    /// <summary>Remembers a project loaded outside the workspace helpers so that it is unloaded on dispose.</summary>
    public void Track(MSProject project) => _loaded.Add(project);

    public MSProject LoadFromFile(string projectPath, string? solutionPackagesDir = null)
    {
        var project = MSBuildExtensions.FromFile(projectPath, SolutionProjectType.KnownToBeMSBuildFormat, solutionPackagesDir);
        _loaded.Add(project);

        return project;
    }

    public void Dispose()
    {
        UnloadProjects(_loaded);

        Directory.Delete(Dir, true);
    }

    /// <summary>Releases projects that a solution loaded into the global collection from this workspace.</summary>
    public static void UnloadProjects(IEnumerable<MSProject> projects)
    {
        var collection = ProjectCollection.GlobalProjectCollection;

        foreach (var project in projects.Select(x => x.Project).Distinct().ToArray())
        {
            if (collection.LoadedProjects.Contains(project))
                collection.UnloadProject(project);
        }
    }

    // A test host is not MSBuild, so on some runtimes no toolset is discovered; register a stand-in because the
    // projects never import anything from the tools path.
    private static void EnsureToolset()
    {
        lock (ToolsetLock)
        {
            var collection = ProjectCollection.GlobalProjectCollection;

            if (collection.GetToolset(collection.DefaultToolsVersion) is not null)
                return;

            collection.AddToolset(new("Current", AppContext.BaseDirectory, collection, string.Empty));
            collection.DefaultToolsVersion = "Current";
        }
    }
}
