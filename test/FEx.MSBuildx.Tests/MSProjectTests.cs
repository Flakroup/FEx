using Microsoft.Build.Construction;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace FEx.MSBuildx.Tests;

/// <summary>Evaluates small generated projects and checks how <see cref="MSProject" /> reads them.</summary>
public sealed class MSProjectTests : IDisposable
{
    private const string Sep = "$([System.IO.Path]::DirectorySeparatorChar)";

    private readonly MSBuildTestWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public void Constructor_ReadsTheIdentityOfTheProject()
    {
        var path = _workspace.WriteProject("Demo.csproj");

        var project = _workspace.Load(path);

        project.Name.ShouldBe("Demo");
        project.AssemblyName.ShouldBe("Demo");
        project.ProjectFile.ShouldBe("Demo.csproj");
        project.FullPath.ShouldBe(path);
        project.ProjectDir.ShouldBe(_workspace.Dir + Path.DirectorySeparatorChar);
        project.ProjectType.ShouldBe(SolutionProjectType.KnownToBeMSBuildFormat);
        project.Project.FullPath.ShouldBe(path);
    }

    [Fact]
    public void Constructor_FallsBackToTheProjectDirectoryProperty_WhenProjectDirIsNotDefined()
    {
        var props = MSBuildTestWorkspace.Props().Replace("<ProjectDir>", "<NotProjectDir>").Replace("</ProjectDir>", "</NotProjectDir>");

        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.ProjectDir.ShouldBe(_workspace.Dir);
    }

    [Fact]
    public void Constructor_ReadsPackagingMetadata()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<PackageId>Demo.Pkg</PackageId><Company>Acme</Company><IsPackable>true</IsPackable><Version>1.2.3</Version>");

        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.PackageId.ShouldBe("Demo.Pkg");
        project.Company.ShouldBe("Acme");
        project.IsPackable.ShouldBeTrue();
        project.Version.ShouldBe("1.2.3");
    }

    [Fact]
    public void Constructor_TreatsTheProjectAsNotPackable_WhenIsPackableIsNotDefined() =>
        _workspace.Load(_workspace.WriteProject("Demo.csproj")).IsPackable.ShouldBeFalse();

    [Theory]
    [InlineData("net10.0", true, false)]
    [InlineData("netstandard2.0", true, false)]
    [InlineData("netcoreapp3.1", true, true)]
    [InlineData("netcoreapp2.1", true, false)]
    [InlineData("net48", false, false)]
    public void Constructor_ClassifiesTheTargetFramework(string targetFramework, bool isNetCore, bool isNetCore3)
    {
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", MSBuildTestWorkspace.Props(targetFramework)));

        project.TargetFramework.ShouldBe(targetFramework);
        project.IsNetCore.ShouldBe(isNetCore);
        project.IsNetCore3.ShouldBe(isNetCore3);
    }

    [Fact]
    public void Constructor_FallsBackToTheTargetFrameworkVersion_WhenNoTargetFrameworkIsDefined()
    {
        var props = MSBuildTestWorkspace.Props(null, extra: "<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>");

        _workspace.Load(_workspace.WriteProject("Demo.csproj", props)).TargetFramework.ShouldBe("v4.8");
    }

    [Fact]
    public void Constructor_ReadsTheRuntimeIdentifierOfANetCoreProjectFromTheSdkProperty()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<NETCoreSdkRuntimeIdentifier>win-x64</NETCoreSdkRuntimeIdentifier><RuntimeIdentifier>linux-x64</RuntimeIdentifier>");

        _workspace.Load(_workspace.WriteProject("Demo.csproj", props)).RuntimeIdentifier.ShouldBe("win-x64");
    }

    [Fact]
    public void Constructor_ReadsTheRuntimeIdentifierOfAFrameworkProjectFromTheProperty()
    {
        var props = MSBuildTestWorkspace.Props("net48", extra: "<NETCoreSdkRuntimeIdentifier>win-x64</NETCoreSdkRuntimeIdentifier><RuntimeIdentifier>win-x86</RuntimeIdentifier>");

        _workspace.Load(_workspace.WriteProject("Demo.csproj", props)).RuntimeIdentifier.ShouldBe("win-x86");
    }

    [Fact]
    public void Constructor_LeavesTheRuntimeIdentifierEmpty_WhenNoneIsDefined() =>
        _workspace.Load(_workspace.WriteProject("Demo.csproj")).RuntimeIdentifier.ShouldBeNull();

    [Fact]
    public void Constructor_ResolvesTheOutputDirectories()
    {
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj"));

        project.OutDir.ShouldBe("bin" + Path.DirectorySeparatorChar + "Debug" + Path.DirectorySeparatorChar);
        project.OutDirPath.ShouldBe(Path.GetFullPath(Path.Combine(_workspace.Dir, project.OutDir)));
        project.BinDir.ShouldBe(Path.GetFullPath(Path.Combine(_workspace.Dir, "bin")));
        project.InitialMSBuildProjectExtensionsPath.ShouldBe(Path.GetFullPath(Path.Combine(_workspace.Dir, "obj")));
    }

    [Fact]
    public void Constructor_FallsBackToTheOutputPath_WhenNoOutDirIsDefined()
    {
        var props = MSBuildTestWorkspace.Props(outDir: null, extra: "<OutputPath>out" + Sep + "Release" + Sep + "</OutputPath>");

        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.OutDir.ShouldBe("out" + Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar);
        project.BinDir.ShouldBe(Path.GetFullPath(Path.Combine(_workspace.Dir, "out")));
    }

    [Fact]
    public void Constructor_UsesTheInitialExtensionsPath_WhenItIsDefined()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<_InitialMSBuildProjectExtensionsPath>$(ProjectDir)custom-obj</_InitialMSBuildProjectExtensionsPath>");

        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.InitialMSBuildProjectExtensionsPath.ShouldBe(Path.GetFullPath(Path.Combine(_workspace.Dir, "custom-obj")));
    }

    [Fact]
    public void Constructor_MovesAnOutDirToTheX64Folder_ForA64BitBuild()
    {
        var props = MSBuildTestWorkspace.Props(outDir: @"bin\Debug\");

        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props), is64Bit: true);

        project.OutDir.ShouldBe(@"bin\x64\Debug\");
    }

    [Fact]
    public void Constructor_KeepsAnOutDirThatAlreadyHasAnX64Folder_ForA64BitBuild()
    {
        var props = MSBuildTestWorkspace.Props(outDir: @"bin\x64\Debug\");

        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props), is64Bit: true);

        project.OutDir.ShouldBe(@"bin\x64\Debug\");
    }

    [Fact]
    public void Constructor_KeepsTheOutDir_ForA32BitBuild()
    {
        var props = MSBuildTestWorkspace.Props(outDir: @"bin\Debug\");

        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.OutDir.ShouldBe(@"bin\Debug\");
    }

    [Fact]
    public void Constructor_CollectsPackageReferencesWithAVersion()
    {
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj",
            items: """
                   <PackageReference Include="Foo" Version="1.2.3" />
                   <PackageReference Include="Pre" Version="1.0.0-beta.2" />
                   <PackageReference Include="NoVersion" />
                   """));

        project.NuGetPackages.Select(x => $"{x.Id} {x.Version}").ShouldBe(["Foo 1.2.3", "Pre 1.0.0-beta.2"], true);
    }

    [Fact]
    public void Constructor_UsesTheLowerBoundOfAVersionRange()
    {
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj",
            items: """<PackageReference Include="Ranged" Version="[2.0.0,3.0.0)" />"""));

        project.NuGetPackages.ShouldHaveSingleItem().Version.ToString().ShouldBe("2.0.0");
    }

    [Fact]
    public void Constructor_Throws_WhenAPackageVersionIsInvalid()
    {
        var path = _workspace.WriteProject("Demo.csproj", items: """<PackageReference Include="Bad" Version="not-a-version" />""");

        Should.Throw<Exception>(() => _workspace.Load(path)).Message.ShouldContain("not-a-version");
    }

    [Fact]
    public void Constructor_DetectsAnAspNetCoreProject()
    {
        var aspNet = _workspace.Load(_workspace.WriteProject("Web.csproj",
            items: """<PackageReference Include="Microsoft.AspNetCore.App" Version="2.2.0" />"""));

        var plain = _workspace.Load(_workspace.WriteProject("Lib.csproj",
            items: """<PackageReference Include="Other" Version="1.0.0" />"""));

        aspNet.IsAspNetCore.ShouldBeTrue();
        plain.IsAspNetCore.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_ExcludesIgnoredItemTypesFromTheIncludedFiles()
    {
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj",
            items: """
                   <Compile Include="Program.cs" />
                   <None Include="readme.txt" />
                   <EmbeddedResource Include="data.resx" />
                   <Reference Include="System.Xml" />
                   <ProjectReference Include="Other.csproj" />
                   <PackageReference Include="Foo" Version="1.0.0" />
                   """));

        project.IncludedFiles.Select(x => x.EvaluatedInclude).ShouldBe(["Program.cs", "readme.txt", "data.resx"], true);
    }

    [Fact]
    public void Constructor_ExcludesItemsUnderTheSolutionPackagesDirectory()
    {
        var path = _workspace.WriteProject("Demo.csproj",
            items: """
                   <Content Include="solpkgs_lib.dll" />
                   <Content Include="local.dll" />
                   """);

        var project = _workspace.Load(path, "solpkgs_");

        project.IncludedFiles.Select(x => x.EvaluatedInclude).ShouldBe(["local.dll"]);
    }

    [Fact]
    public void Constructor_ExcludesItemsUnderTheNuGetPackageRoot()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<NuGetPackageRoot>nugetroot_</NuGetPackageRoot>");
        var path = _workspace.WriteProject("Demo.csproj",
            props,
            """
            <Content Include="nugetroot_lib.dll" />
            <Content Include="local.dll" />
            """);

        var project = _workspace.Load(path);

        project.NuGetPackageRoot.ShouldBe("nugetroot_");
        project.IncludedFiles.Select(x => x.EvaluatedInclude).ShouldBe(["local.dll"]);
    }

    [Fact]
    public void Constructor_ExposesTheEvaluatedItems()
    {
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", items: """<Compile Include="Program.cs" />"""));

        project.AllEvaluatedItems.Count.ShouldBe(project.Project.AllEvaluatedItems.Count);
        project.AllEvaluatedItems.Select(x => x.EvaluatedInclude).ShouldContain("Program.cs");
    }

    [Fact]
    public void Constructor_TrimsAndSplitsTheMSBuildAllProjectsList()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<MSBuildAllProjects>one.props ; two.props</MSBuildAllProjects>");

        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.MSBuildAllProjects.ShouldBe(["Demo.csproj", "one.props", "two.props"]);
    }

    [Fact]
    public void Constructor_StripsTheProjectDirectoryFromTheMSBuildAllProjectsList()
    {
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj"));

        project.MSBuildAllProjects.ShouldBe(["Demo.csproj"]);
    }

    [Fact]
    public void Constructor_AddsPropertiesThatOnlyAppearInConditions()
    {
        var props = MSBuildTestWorkspace.Props() + """<Flavor Condition="'$(Flavor)' == 'Blue'">Blue</Flavor>""";

        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.PropertiesDictionary["Flavor"].ShouldBe("Blue");
    }

    [Fact]
    public void Constructor_ReplacesAPropertyValueWithTheValuesItIsComparedWith()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<Flavor>Red</Flavor>")
                    + """<Shade Condition="'$(Flavor)' == 'Blue'">dark</Shade>""";

        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.PropertiesDictionary["Flavor"].ShouldBe("Blue");
    }

    [Fact]
    public void Constructor_ListsTheConfigurations_WhenConfigurationsIsDefined()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<Configurations>Debug;Release;Staging</Configurations>");

        _workspace.Load(_workspace.WriteProject("Demo.csproj", props)).Configurations.ShouldBe(["Debug", "Release", "Staging"]);
    }

    [Fact]
    public void Constructor_FallsBackToTheCurrentConfiguration_WhenConfigurationsIsNotDefined() =>
        _workspace.Load(_workspace.WriteProject("Demo.csproj")).Configurations.ShouldBe(["Debug"]);

    [Fact]
    public void Constructor_HasNoPublishDirs_WhenNoPublishDirectoryIsDefined()
    {
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj"));

        project.PublishDirName.ShouldBeNull();
        project.PublishDirs.ShouldBeNull();
    }

    [Fact]
    public void Constructor_BuildsPublishDirsPerConfiguration_FromThePublishDirName()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<Configurations>Debug;Release</Configurations><PublishDirName>publish</PublishDirName>");

        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.PublishDirName.ShouldBe("publish");
        project.PublishDirs.ShouldNotBeNull();
        project.PublishDirs["Debug"].ShouldBe(Path.Combine(project.BinDir, "Debug", "publish"));
        project.PublishDirs["Release"].ShouldBe(Path.Combine(project.BinDir, "Release", "publish"));
    }

    [Fact]
    public void Constructor_IncludesTheRuntimeIdentifierInThePublishDirs()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<NETCoreSdkRuntimeIdentifier>win-x64</NETCoreSdkRuntimeIdentifier><PublishDirName>publish</PublishDirName>");

        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.PublishDirs.ShouldNotBeNull()["Debug"].ShouldBe(Path.Combine(project.BinDir, "Debug", "win-x64", "publish"));
    }

    [Fact]
    public void Constructor_DerivesThePublishDirName_FromThePublishDirProperty()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<PublishDir>out" + Sep + "deploy</PublishDir>");

        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.PublishDirName.ShouldBe("deploy");
        project.PublishDirs.ShouldNotBeNull().ShouldContainKey("Debug");
    }

    [Fact]
    public void GetProjectOutputPath_ReturnsThePublishDirOfAKnownConfiguration()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<Configurations>Debug;Release</Configurations><PublishDirName>publish</PublishDirName>");
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.GetProjectOutputPath("Release").ShouldBe(Path.Combine(project.BinDir, "Release", "publish"));
    }

    [Fact]
    public void GetProjectOutputPath_FallsBackToTheFirstConfiguration_ForAnUnknownConfigurationWithPublishDirs()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<Configurations>Debug;Release</Configurations><PublishDirName>publish</PublishDirName>");
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.GetProjectOutputPath("Other").ShouldBe(Path.Combine(project.BinDir, "Debug"));
    }

    [Fact]
    public void GetProjectOutputPath_ReturnsTheBinFolderOfTheConfiguration_WithoutPublishDirs()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<Configurations>Debug;Release</Configurations>");
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", props));

        project.GetProjectOutputPath("Release").ShouldBe(Path.Combine(project.BinDir, "Release"));
        project.GetProjectOutputPath("Other").ShouldBe(Path.Combine(project.BinDir, "Debug"));
    }

    [Fact]
    public void SetPackageVersion_WritesTheVersionBackToTheProjectFile()
    {
        var path = _workspace.WriteProject("Demo.csproj");
        var project = _workspace.Load(path);

        project.SetPackageVersion("4.5.6");

        project.Project.GetPropertyValue("Version").ShouldBe("4.5.6");
        File.ReadAllText(path).ShouldContain("<Version>4.5.6</Version>");
    }

    [Fact]
    public void Files_ListsTheProjectFilesRelativeToTheProjectDirectory_WithoutBuildOutput()
    {
        var project = LoadLayout();

        project.Files.ShouldBe(
            ["Demo.csproj", "Program.cs", "notes.txt", Path.Combine("sub", "other.txt")],
            true);
    }

    [Fact]
    public void IgnoredDirectories_HoldsTheExistingIntermediateAndOutputDirectories()
    {
        var project = LoadLayout();

        project.IgnoredDirectories.ShouldBe(
            [project.InitialMSBuildProjectExtensionsPath, project.OutDirPath],
            true);
    }

    [Fact]
    public void IgnoredDirectories_IsEmpty_WhenNeitherDirectoryExists() =>
        _workspace.Load(_workspace.WriteProject("Demo.csproj")).IgnoredDirectories.ShouldBeEmpty();

    [Fact]
    public void IgnoredFiles_ListsFilesThatNoProjectItemIncludes()
    {
        var project = LoadLayout();

        project.IgnoredFiles.Select(x => x.RelativePath).ShouldBe(["notes.txt", Path.Combine("sub", "other.txt")], true);
    }

    [Fact]
    public void IgnoredFiles_DescribesARootFile()
    {
        var project = LoadLayout();

        var notes = project.IgnoredFiles.Single(x => x.RelativePath == "notes.txt");

        notes.Project.ShouldBeSameAs(project);
        notes.FullPath.ShouldBe(Path.Combine(project.ProjectDir, "notes.txt"));
        notes.ItemType.ShouldBe("Ignored");
        notes.Exists.ShouldBeTrue();
        notes.Length.ShouldBe(5L);
        notes.Extension.ShouldBe(".txt");
    }

    [Fact]
    public void IgnoredFiles_HasNoFullPathForAFileInASubdirectory()
    {
        var project = LoadLayout();

        var nested = project.IgnoredFiles.Single(x => x.RelativePath == Path.Combine("sub", "other.txt"));

        nested.FullPath.ShouldBeNull();
        nested.Exists.ShouldBeFalse();
        nested.Length.ShouldBe(0L);
        nested.Extension.ShouldBeNull();
    }

    [Fact]
    public void MissingFiles_ListsIncludedItemsThatAreNotOnDisk()
    {
        var project = LoadLayout();

        var missing = project.MissingFiles.ShouldHaveSingleItem();

        missing.RelativePath.ShouldBe("Missing.cs");
        missing.FullPath.ShouldBe(Path.Combine(project.ProjectDir, "Missing.cs"));
        missing.ItemType.ShouldBe("Compile");
        missing.Exists.ShouldBeFalse();
        missing.Length.ShouldBe(0L);
        missing.Extension.ShouldBe(".cs");
    }

    private MSProject LoadLayout()
    {
        _workspace.WriteFile("Program.cs", "class P {}");
        _workspace.WriteFile("notes.txt", "notes");
        _workspace.WriteFile(Path.Combine("sub", "other.txt"), "other");
        _workspace.WriteFile(Path.Combine("obj", "project.assets.json"), "{}");
        _workspace.WriteFile(Path.Combine("bin", "Debug", "out.dll"), "bin");

        return _workspace.Load(_workspace.WriteProject("Demo.csproj",
            items: """
                   <Compile Include="Program.cs" />
                   <Compile Include="Missing.cs" />
                   """));
    }
}
