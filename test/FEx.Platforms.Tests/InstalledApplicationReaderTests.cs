using Microsoft.Win32;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

// These tests use fakes only; the registry enums are referenced as plain values.
#pragma warning disable CA1416

namespace FEx.Platforms.Tests;

public sealed class InstalledApplicationReaderTests
{
    [Fact]
    public void Map_Reads_All_Values_And_Source()
    {
        var values = new Dictionary<string, object?>
        {
            ["DisplayName"] = "Contoso",
            ["DisplayVersion"] = "1.2.3",
            ["Publisher"] = "Contoso Ltd",
            ["InstallLocation"] = @"C:\Contoso",
            ["UninstallString"] = @"C:\Contoso\unins.exe",
            ["InstallDate"] = "20240229"
        };

        var app = InstalledApplicationReader.Map("{GUID}",
            values.GetValueOrDefault,
            RegistryHive.LocalMachine,
            RegistryView.Registry64);

        app.KeyName.ShouldBe("{GUID}");
        app.DisplayName.ShouldBe("Contoso");
        app.DisplayVersion.ShouldBe("1.2.3");
        app.Publisher.ShouldBe("Contoso Ltd");
        app.InstallLocation.ShouldBe(@"C:\Contoso");
        app.UninstallString.ShouldBe(@"C:\Contoso\unins.exe");
        app.InstallDate.ShouldBe(new DateTime(2024, 2, 29));
        app.Hive.ShouldBe(RegistryHive.LocalMachine);
        app.View.ShouldBe(RegistryView.Registry64);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a date")]
    [InlineData("20241340")]
    public void Map_Treats_Missing_Blank_Or_Invalid_Values_As_Null(string? installDate)
    {
        var app = InstalledApplicationReader.Map("k",
            name => name == "InstallDate"
                ? installDate
                : "   ",
            RegistryHive.LocalMachine,
            RegistryView.Registry32);

        app.InstallDate.ShouldBeNull();
        app.DisplayName.ShouldBeNull();
        app.Publisher.ShouldBeNull();
    }

    [Fact]
    public void ReadAll_Disposes_Every_Opened_Entry()
    {
        var opened = new List<FakeEntry>();

        var apps = InstalledApplicationReader.ReadAll(["a", "b", "c"],
            name =>
            {
                var entry = new FakeEntry(name);
                opened.Add(entry);

                return entry;
            },
            RegistryHive.LocalMachine,
            RegistryView.Registry32);

        apps.Select(x => x.DisplayName).ShouldBe(["a", "b", "c"]);
        opened.Count.ShouldBe(3);
        opened.ShouldAllBe(x => x.IsDisposed);
    }

    [Fact]
    public void ReadAll_Disposes_Entry_When_Mapping_Throws()
    {
#pragma warning disable IDISP001 // Disposed by the reader under test, which is what is asserted
        var entry = new FakeEntry("a", true);
#pragma warning restore IDISP001

        Should.Throw<InvalidOperationException>(() => InstalledApplicationReader.ReadAll(["a"],
            _ => entry,
            RegistryHive.LocalMachine,
            RegistryView.Registry32));

        entry.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public void ReadAll_Skips_Keys_That_Cannot_Be_Opened()
    {
        var apps = InstalledApplicationReader.ReadAll(["gone"],
            _ => null,
            RegistryHive.LocalMachine,
            RegistryView.Registry32);

        apps.ShouldBeEmpty();
    }

    [Fact]
    public void Sources_Read_The_Plain_Uninstall_Path_Under_Both_Views_Without_Wow6432Node()
    {
        const string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        InstalledApplicationReader.Sources.ShouldBe([
            new UninstallSource(RegistryView.Registry64, path),
            new UninstallSource(RegistryView.Registry32, path)
        ]);
    }

    [Fact]
    public void ReadAll_Reads_Each_Source_Tags_Its_View_And_Disposes_Roots_And_Entries()
    {
        var roots = new List<FakeRoot>();

        var apps = InstalledApplicationReader.ReadAll(source =>
        {
            var root = new FakeRoot(source.View, "x", "y");
            roots.Add(root);

            return root;
        });

        apps.Select(x => (x.DisplayName, x.View))
            .ShouldBe([
                ("Registry64:x", RegistryView.Registry64),
                ("Registry64:y", RegistryView.Registry64),
                ("Registry32:x", RegistryView.Registry32),
                ("Registry32:y", RegistryView.Registry32)
            ]);
        roots.Count.ShouldBe(2);
        roots.ShouldAllBe(x => x.IsDisposed);
        roots.SelectMany(x => x.Entries).ShouldAllBe(x => x.IsDisposed);
    }

    [Fact]
    public void ReadAll_Skips_Sources_That_Do_Not_Exist()
    {
        InstalledApplicationReader.ReadAll(_ => null).ShouldBeEmpty();
    }

    private sealed class FakeRoot(RegistryView view, params string[] names) : IUninstallRoot
    {
        public List<FakeEntry> Entries { get; } = [];

        public bool IsDisposed { get; private set; }

        public IReadOnlyList<string> GetEntryNames() => names;

        public IUninstallEntry? OpenEntry(string name)
        {
#pragma warning disable IDISP001 // Tracked in Entries, disposed by the reader under test
            var entry = new FakeEntry($"{view}:{name}");
#pragma warning restore IDISP001
            Entries.Add(entry);

            return entry;
        }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class FakeEntry(string displayName, bool throwOnRead = false) : IUninstallEntry
    {
        public bool IsDisposed { get; private set; }

        public object? GetValue(string name) =>
            throwOnRead
                ? throw new InvalidOperationException()
                : name == "DisplayName"
                    ? displayName
                    : null;

        public void Dispose() => IsDisposed = true;
    }
}
