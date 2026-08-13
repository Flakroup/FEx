using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
using FEx.Agnostics.Collections.Concurrent;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using NuGet;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.MSBuildx;

public class MSProject
{
    private const string PkgsConfStr = "packages.config";
    public static string PackageRefStr { get; } = "PackageReference";

    public static ImmutableHashSet<string> IgnoredItemsNames { get; } = ImmutableHashSet.Create("Reference",
        PackageRefStr,
        "ProjectReference",
        "Folder",
        "BootstrapperPackage",
        "ProjectCapability",
        "PropertyPageSchema",
        "_OutputPathItem",
        "_UnmanagedRegistrationCache",
        "_ResolveComReferenceCache",
        "AppConfigFileDestination",
        "IntermediateAssembly",
        "CopyUpToDateMarker",
        "_DebugSymbolsIntermediatePath",
        "_DebugSymbolsOutputPath",
        "_DeploymentManifestEntryPoint",
        "ApplicationManifest",
        "_ApplicationManifestFinal",
        "DeployManifest",
        "_DeploymentIntermediateTrustInfoFile",
        "BuiltProjectOutputGroupKeyOutput",
        "DebugSymbolsProjectOutputGroupOutput",
        "AvailableItemName",
        "Clean",
        "XamlBuildTaskTypeGenerationExtensionName",
        "NuGetPreprocessorValue",
        "_ExplicitReference",
        "SupportedTargetFramework",
        "_SDKImplicitReference",
        "AdditionalDesignTimeBuildInput",
        "PreprocessorValue",
        "GenerateRuntimeConfigurationFilesInputs",
        "Service",
        "WCFMetadata",
        "CustomAdditionalCompileInputs",
        "AppDesigner",
        "WCFMetadataStorage",
        "AppxHashUri",
        "AppxSystemBinary",
        "AppxReservedFileName",
        "AppxManifestFileNameQuery",
        "AppxManifestMetadata",
        "AppxManifestMetaData",
        "PlatformVersionDescription",
        "_ImplicitPackageReferenceCheck",
        "_ImplicitPackageReference",
        "AdditionalFiles",
        "_IISApplicationPool",
        "_MSDeployArchiveDir",
        "_MSDeployPackageFile",
        "_MSDeployPackageLocation",
        "_WPPSupports",
        "COMReference",
        "WebPublishExtnsionsToExcludeItem",
        "RoslynCompilerFiles",
        "PackageConflictOverrides",
        "BundledDotNetCliToolReference",
        "DocFileItem",
        "FinalDocFile",
        "DocumentationProjectOutputGroupOutput",
        "WeaverFiles",
        "Import",
        "AdditionalProbingPath",
        "DotNetCliToolReference",
        "ExpectedPlatformPackages",
        "RuntimeHostConfigurationOption",
        "RazorIntermediateAssembly",
        "_RazorDebugSymbolsIntermediatePath",
        "UpToDateCheckBuilt",
        "_AspNetCoreAllReference",
        "_ExplicitAspNetCoreAllReference",
        "ProjectReferenceTargets",
        "ImplicitPackageReferenceVersion",
        "SupportedNETCoreAppTargetFramework",
        "SupportedNETFrameworkTargetFramework",
        "SupportedNETStandardTargetFramework",
        "PotentialEditorConfigFiles",
        "KnownFrameworkReference",
        "KnownAppHostPack",
        "FrameworkReference",
        "_AllDirectoriesAbove",
        "_AndroidAssemblySkipCases",
        "AndroidCustomMetaDataForReferences",
        "AndroidMinimumSupportedApiLevel",
        "RuntimeReferenceOnlySDKDependencies");

    public static ConcurrentList<string> AcceptedItemsNames { get; } = new(
        new[] { ProjectItemType.None, ProjectItemType.EmbeddedResource }
            .Select(x => x.GetEnumValueDescription() ?? x.ToString()));

    public Project Project { get; }
    public SolutionProjectType ProjectType { get; }
    public IList<MSProjectItem> IncludedFiles { get; }
    public Dictionary<string, string> PropertiesDictionary { get; }
    public ConcurrentList<string> Files { get; }
    public IList<string> IgnoredDirectories { get; }
    public IList<DiffResult> IgnoredFiles { get; }

    public string Name { get; }
    public string AssemblyName { get; }
    public string ProjectDir { get; }
    public string ProjectFile { get; }
    public string OutDir { get; }
    public string OutDirPath { get; }
    public string? NuGetPackageRoot { get; }
    public IList<string> MSBuildAllProjects { get; }
    public string TargetFramework { get; }
    public bool IsNetCore3 { get; }
    public string InitialMSBuildProjectExtensionsPath { get; }
    public string FullPath { get; }
    public ConcurrentList<DiffResult> MissingFiles { get; }
    public string BinDir { get; }
    public string? PublishDirName { get; }
    public string? RuntimeIdentifier { get; }
    public IList<string> Configurations { get; }
    public IDictionary<string, string>? PublishDirs { get; }
    public HashSet<PackageIdentity> NuGetPackages { get; }
    public bool IsAspNetCore { get; }
    public bool IsNetCore { get; }
    public string Company { get; }
    public string PackageId { get; }
    public bool IsPackable { get; }
    public string Version { get; }
    public ICollection<ProjectItem> AllEvaluatedItems => Project.AllEvaluatedItems;

    private HashSet<string> IncludedFilesPaths { get; }

    public MSProject(Project project,
                     SolutionProjectType projectType,
                     string? solutionPackagesDir = null,
                     bool is64Bit = false)
    {
        Project = project;
        ProjectType = projectType;
        PropertiesDictionary = Project.Properties.OrderBy(x => x.Name).ToDictionary(x => x.Name, x => x.EvaluatedValue);

        foreach (var p in project.ConditionedProperties)
            PropertiesDictionary.AddOrUpdateValue(p.Key, () => string.Join(";", p.Value));

        IncludedFiles = new ConcurrentList<MSProjectItem>();
        FullPath = Project.FullPath;

        Name = PropertiesDictionary["MSBuildProjectName"];
        NuGetPackageRoot = PropertiesDictionary.TryGetKeyValue<string, string>("NuGetPackageRoot");
        ProjectFile = PropertiesDictionary["MSBuildProjectFile"];
        AssemblyName = PropertiesDictionary["AssemblyName"];

        ProjectDir = PropertiesDictionary.TryGetKeyValue<string, string>("ProjectDir")
                     ?? PropertiesDictionary.TryGetKeyValue<string, string>("MSBuildProjectDirectory");

        MSBuildAllProjects = PropertiesDictionary["MSBuildAllProjects"]
            .Split(';')
            .Select(x => x.Trim().Replace(ProjectDir, string.Empty))
            .ToArray();

        TargetFramework = PropertiesDictionary.TryGetKeyValue<string, string>("TargetFramework")
                          ?? PropertiesDictionary["TargetFrameworkVersion"];

        IsNetCore = TargetFramework.StartsWith("netcoreapp") || !TargetFramework.StartsWith("net4");
        IsNetCore3 = TargetFramework.StartsWith("netcoreapp3.");
        OutDir = PropertiesDictionary.TryGetKeyValue<string, string>("OutDir") ?? PropertiesDictionary.TryGetKeyValue<string, string>("OutputPath");

        if (is64Bit && !OutDir.Contains("\\x64\\"))
        {
            var relPath = OutDir.Replace("bin\\", "");
            OutDir = "bin\\x64\\" + relPath;
        }

        var initialMSBuildProjectExtensionsPath =
            PropertiesDictionary.TryGetKeyValue<string, string>("_InitialMSBuildProjectExtensionsPath")
            ?? Path.Combine(ProjectDir, "obj");

        InitialMSBuildProjectExtensionsPath = Path.GetFullPath(initialMSBuildProjectExtensionsPath);

        PackageId = PropertiesDictionary.TryGetKeyValue<string, string>("PackageId");
        Company = PropertiesDictionary.TryGetKeyValue<string, string>("Company");
        IsPackable = bool.Parse(PropertiesDictionary.TryGetKeyValue<string, string>("IsPackable", "false"));
        Version = PropertiesDictionary.TryGetKeyValue<string, string>("Version");
        NuGetPackages = [];

        Parallel.ForEach(AllEvaluatedItems.Where(x =>
                    !IgnoredItemsNames.Contains(x.ItemType)
                    && (solutionPackagesDir is null || !x.EvaluatedInclude.StartsWith(solutionPackagesDir))
                    && (NuGetPackageRoot is null || !x.EvaluatedInclude.StartsWith(NuGetPackageRoot)))
                .ToArray(),
            item => IncludedFiles.Add(new(item, this)));

        //Dictionary<string, List<MSProjectItem>> typedProjectFiles = IncludedFiles.GroupBy(x => x.ItemType).ToDictionary(x => x.Key, x => x.ToList());
        NuGetPackages.AddRangeToCollection(AllEvaluatedItems
            .Where(x => x.ItemType == PackageRefStr && x.DirectMetadata.Any(y => y.Name == "Version"))
            .Select(GetPackageIdentity)
            .ToArray());

        IsAspNetCore = NuGetPackages.Any(x => x.Id == "Microsoft.AspNetCore.App");

        var outRoot = OutDir.GetPathParts().First();
        BinDir = Path.GetFullPath(Path.Combine(ProjectDir, outRoot));
        var isPackagesConfig = AllEvaluatedItems.Any(x => x.EvaluatedInclude.Contains(PkgsConfStr));

        if (isPackagesConfig)
        {
            var pkgsConf = new FileInfo(Path.Combine(ProjectDir, PkgsConfStr));
            var file = new PackageReferenceFile(pkgsConf.FullName);

            NuGetPackages.AddRangeToCollection(file.GetPackageReferences()
                .Select(x => new PackageIdentity(x.Id, NuGetVersion.Parse(x.Version.ToString())))
                .ToArray());
        }

        // Defensive removal of any null entries left by the parallel population above.
        while (IncludedFiles.Contains(null!))
            IncludedFiles.Remove(null!);

        IncludedFilesPaths = [.. IncludedFiles.Select(x => x.EvaluatedInclude)];

        Configurations =
            (PropertiesDictionary.TryGetKeyValue<string, string>("Configurations") ?? PropertiesDictionary["Configuration"]).Split(';');

        PublishDirName = PropertiesDictionary.TryGetKeyValue<string, string>("PublishDirName")
                         ?? (PropertiesDictionary.TryGetKeyValue<string, string>("PublishDir") is not null
                             ? new DirectoryInfo(Path.Combine(ProjectDir,
                                 PropertiesDictionary.TryGetKeyValue<string, string>("PublishDir"))).Name
                             : null);

        RuntimeIdentifier = IsNetCore
            ? PropertiesDictionary.TryGetKeyValue<string, string>("NETCoreSdkRuntimeIdentifier")
            : PropertiesDictionary.TryGetKeyValue<string, string>("RuntimeIdentifier");

        if (PublishDirName is not null)
            PublishDirs = Configurations.ToDictionary(x => x,
                x => RuntimeIdentifier is not null
                    ? Path.Combine(BinDir, x, RuntimeIdentifier, PublishDirName)
                    : Path.Combine(BinDir, x, PublishDirName));

        IgnoredDirectories = new List<string>();

        if (Directory.Exists(InitialMSBuildProjectExtensionsPath))
            IgnoredDirectories.Add(InitialMSBuildProjectExtensionsPath);

        OutDirPath = Path.GetFullPath(Path.IsPathRooted(OutDir)
            ? OutDir
            : Path.Combine(ProjectDir, OutDir));

        if (Directory.Exists(OutDirPath))
            IgnoredDirectories.Add(OutDirPath);

        Files = new(Directory.GetFiles(ProjectDir, "*.*", SearchOption.AllDirectories)
            .Except(IgnoredDirectories.SelectMany(x => Directory.GetFiles(x, "*.*", SearchOption.AllDirectories)))
            .Select(x => x.Replace(ProjectDir, string.Empty)));

        IgnoredFiles = new List<DiffResult>();
        RefreshIgnoredFiles();
        MissingFiles = [];
        RefreshMissingFiles();
    }

    public string GetProjectOutputPath(string buildConfiguration) =>
        PublishDirs?.TryGetKeyValue<string, string>(buildConfiguration)
        ?? Path.Combine(BinDir, Configurations.FirstOrDefault(x => x == buildConfiguration) ?? Configurations[0]);

    public void SetPackageVersion(string version)
    {
        Project.SetProperty("Version", version);
        Project.Save();
    }

    private static PackageIdentity GetPackageIdentity(ProjectItem item)
    {
        var versionString = item.DirectMetadata.First(x => x.Name == "Version").EvaluatedValue;

        if (!NuGetVersion.TryParse(versionString, out var version))
        {
            if (VersionRange.TryParse(versionString, out var versionRange))
                version = versionRange.MinVersion;
            else
                throw new($"Provided version string {versionString} is invalid");
        }

        return new(item.UnevaluatedInclude, version);
    }

    private void RefreshIgnoredFiles()
    {
        IgnoredFiles.Clear();

        IgnoredFiles.AddRangeToList(Files.Except(MSBuildAllProjects)
            .Where(x => !IncludedFilesPaths.Any(y => y.Equals(x, StringComparison.OrdinalIgnoreCase)))
            .Select(item => new DiffResult(this, item)));
    }

    private void RefreshMissingFiles()
    {
        MissingFiles.Clear();

        MissingFiles.AddRange(IncludedFiles
            .Where(x => !Files.Any(y => y.Equals(x.EvaluatedInclude, StringComparison.OrdinalIgnoreCase)))
            .Select(item => new DiffResult(this, null, item)));
    }
}