using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Xml;
using FEx.MSBuildx;
using Xunit;

namespace FEx.MSBuildx.Tests;


public sealed class PackagesConfigFileTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("fex-pkgconf-").FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public void Read_ReturnsEveryEntry_WhenAPackageIdRepeatsAcrossVersions()
    {
        var file = Write("""
            <packages>
              <package id="A" version="1.0.0" targetFramework="net45" />
              <package id="A" version="2.0.0" targetFramework="net46" />
            </packages>
            """);

        var result = PackagesConfigFile.Read(file);

        result.Select(x => $"{x.Id} {x.Version}").ShouldBe(["A 1.0.0", "A 2.0.0"]);
    }

    [Fact]
    public void Read_ReturnsBothEntries_WhenAPackageIdAndVersionRepeatAcrossFrameworks()
    {
        var file = Write("""
            <packages>
              <package id="A" version="1.0.0" targetFramework="net45" />
              <package id="A" version="1.0.0" targetFramework="net46" />
            </packages>
            """);

        PackagesConfigFile.Read(file).Count.ShouldBe(2);
    }

    [Fact]
    public void Read_KeepsThePrereleaseLabel()
    {
        var file = Write("""<packages><package id="A" version="1.2.3-beta.4" /></packages>""");

        var package = PackagesConfigFile.Read(file).ShouldHaveSingleItem();

        package.Version.ToString().ShouldBe("1.2.3-beta.4");
        package.Version.IsPrerelease.ShouldBeTrue();
    }

    [Fact]
    public void Read_ReturnsNoPackages_WhenTheFileDoesNotExist() =>
        PackagesConfigFile.Read(new FileInfo(Path.Combine(_dir, "missing.config"))).ShouldBeEmpty();

    [Fact]
    public void Read_RejectsAnInlineDtd()
    {
        var file = Write("""
            <!DOCTYPE packages [<!ENTITY a "Injected.Pkg">]>
            <packages><package id="&a;" version="1.0.0" /></packages>
            """);

        Should.Throw<XmlException>(() => PackagesConfigFile.Read(file));
    }

    private FileInfo Write(string xml)
    {
        var path = Path.Combine(_dir, "packages.config");
        File.WriteAllText(path, xml);

        return new FileInfo(path);
    }
}
