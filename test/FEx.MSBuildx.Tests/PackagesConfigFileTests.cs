using Shouldly;
using System;
using System.Linq;
using System.Xml;
using Xunit;

namespace FEx.MSBuildx.Tests;

/// <summary>
/// Package entries of a legacy <c>packages.config</c> as they reach <see cref="MSProject.NuGetPackages" />, which is the
/// only caller of the reader.
/// </summary>
public sealed class PackagesConfigFileTests : IDisposable
{
    private const string PackagesConfigItem = """<None Include="packages.config" />""";

    private readonly MSBuildTestWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public void NuGetPackages_ContainsEveryEntry_WhenAPackageIdRepeatsAcrossVersions()
    {
        _workspace.WriteFile("packages.config", """
            <packages>
              <package id="A" version="1.0.0" targetFramework="net45" />
              <package id="A" version="2.0.0" targetFramework="net46" />
            </packages>
            """);

        var project = LoadWithPackagesConfig();

        project.NuGetPackages.Select(x => $"{x.Id} {x.Version}").ShouldBe(["A 1.0.0", "A 2.0.0"], true);
    }

    [Fact]
    public void NuGetPackages_Loads_WhenAPackageIdAndVersionRepeatAcrossFrameworks()
    {
        _workspace.WriteFile("packages.config", """
            <packages>
              <package id="A" version="1.0.0" targetFramework="net45" />
              <package id="A" version="1.0.0" targetFramework="net46" />
            </packages>
            """);

        var project = LoadWithPackagesConfig();

        project.NuGetPackages.Select(x => $"{x.Id} {x.Version}").ShouldBe(["A 1.0.0"]);
    }

    [Fact]
    public void NuGetPackages_KeepsThePrereleaseLabel()
    {
        _workspace.WriteFile("packages.config", """<packages><package id="A" version="1.2.3-beta.4" /></packages>""");

        var package = LoadWithPackagesConfig().NuGetPackages.ShouldHaveSingleItem();

        package.Version.ToString().ShouldBe("1.2.3-beta.4");
        package.Version.IsPrerelease.ShouldBeTrue();
    }

    [Fact]
    public void NuGetPackages_IsEmpty_WhenThePackagesConfigFileDoesNotExist() =>
        LoadWithPackagesConfig().NuGetPackages.ShouldBeEmpty();

    [Fact]
    public void NuGetPackages_MergesPackagesConfigEntriesWithPackageReferences()
    {
        _workspace.WriteFile("packages.config", """<packages><package id="Legacy" version="1.0.0" /></packages>""");

        var project = LoadWithPackagesConfig("""<PackageReference Include="Modern" Version="2.0.0" />""");

        project.NuGetPackages.Select(x => x.Id).ShouldBe(["Legacy", "Modern"], true);
    }

    [Fact]
    public void Constructor_RejectsAnInlineDtd()
    {
        _workspace.WriteFile("packages.config", """
            <!DOCTYPE packages [<!ENTITY a "Injected.Pkg">]>
            <packages><package id="&a;" version="1.0.0" /></packages>
            """);

        Should.Throw<XmlException>(() => LoadWithPackagesConfig());
    }

    [Fact]
    public void Constructor_RejectsAnEntryWithoutAnId()
    {
        _workspace.WriteFile("packages.config", """<packages><package version="1.0.0" /></packages>""");

        Should.Throw<XmlException>(() => LoadWithPackagesConfig()).Message.ShouldContain("id");
    }

    [Fact]
    public void Constructor_RejectsAnEntryWithoutAVersion()
    {
        _workspace.WriteFile("packages.config", """<packages><package id="A" /></packages>""");

        Should.Throw<XmlException>(() => LoadWithPackagesConfig()).Message.ShouldContain("version");
    }

    private MSProject LoadWithPackagesConfig(string extraItems = "") =>
        _workspace.Load(_workspace.WriteProject("Demo.csproj", items: PackagesConfigItem + extraItems));
}
