using FEx.AppSettings.Extensions;
using Microsoft.Extensions.Configuration;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace FEx.AppSettings.Tests;

/// <summary>Binding configuration to objects (Microsoft binder and Json.NET) and verifying required settings.</summary>
[Collection(ProcessStateCollection.Name)]
public sealed class FExConfigurationExtensionsTests : IDisposable
{
    private const string SettingsJson = """
        {
          "App": { "Name": "demo", "Port": 8080, "Nested": { "Flag": true } },
          "Other": { "Name": "other" },
          "Root": "r"
        }
        """;

    private readonly TempDirectory _dir = new();
    private readonly string _originalCurrentDirectory = Directory.GetCurrentDirectory();

    public FExConfigurationExtensionsTests() => File.WriteAllText(_dir.File("appsettings.json"), SettingsJson);

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalCurrentDirectory);
        _dir.Dispose();
    }

    private static IConfigurationSection Section(string name, params (string Key, string Value)[] values)
    {
        var data = new Dictionary<string, string?>();

        foreach (var (key, value) in values)
            data[key] = value;

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build().GetSection(name);
    }

    #region VerifyAppSettings

    [Fact]
    public void VerifyAppSettings_WhenEveryKeyHasAValue_ReturnsTheSameInstance()
    {
        Sample sample = new() { Name = "n", Port = 1, Inner = new() { Value = "v" } };

        sample.VerifyAppSettings(x => x.Name!, x => x.Inner!.Value!).ShouldBeSameAs(sample);
    }

    [Fact]
    public void VerifyAppSettings_WithNoKeys_ReturnsTheInstance()
    {
        Sample sample = new();

        sample.VerifyAppSettings().ShouldBeSameAs(sample);
    }

    [Fact]
    public void VerifyAppSettings_NamesASingleMissingKey_WithHas()
    {
        Sample sample = new() { Name = "n" };

        var ex = Should.Throw<ArgumentNullException>(() => sample.VerifyAppSettings(x => x.Name!, x => x.Other!));

        ex.Message.ShouldContain("Other has no value");
    }

    [Fact]
    public void VerifyAppSettings_NamesEveryMissingKey_WithHave()
    {
        Sample sample = new();

        var ex = Should.Throw<ArgumentNullException>(() => sample.VerifyAppSettings(x => x.Name!, x => x.Other!));

        ex.Message.ShouldContain("Name, Other have no value");
    }

    [Fact]
    public void VerifyAppSettings_ForANestedMemberUnderANullParent_ReportsTheFullPath()
    {
        Sample sample = new();

        var ex = Should.Throw<ArgumentNullException>(() => sample.VerifyAppSettings(x => x.Inner!.Value!));

        ex.Message.ShouldContain("Inner.Value has no value");
    }

    [Fact]
    public void VerifyAppSettings_SeesThroughBoxingOfAValueTypeMember()
    {
        Sample sample = new() { Port = 5 };

        sample.VerifyAppSettings(x => x.Port).ShouldBeSameAs(sample);
    }

    [Fact]
    public void VerifyAppSettings_ForAValueTypeMemberUnderANullParent_ReportsThePathWithoutTheBoxing()
    {
        Sample sample = new();

        var ex = Should.Throw<ArgumentNullException>(() => sample.VerifyAppSettings(x => x.Inner!.Number));

        ex.Message.ShouldContain("Inner.Number has no value");
    }

    [Fact]
    public void VerifyAppSettings_ForAnExpressionThatIsNotAMemberPath_ReportsItAsUnsupported()
    {
        Sample sample = new();

        var ex = Should.Throw<InvalidOperationException>(() => sample.VerifyAppSettings(x => x.Compute()!));

        ex.Message.ShouldStartWith("Unsupported expression type: Call");
    }

    [Fact]
    public void VerifyAppSettings_ForANullInstance_Throws()
    {
        Sample sample = null!;

        Should.Throw<ArgumentNullException>(() => sample.VerifyAppSettings(x => x.Name!));
    }

    #endregion

    #region GetBindedConfiguration

    [Fact]
    public void GetBindedConfiguration_BindsTheWholeFile_WhenNoSectionIsGiven()
    {
        var config = FExConfigurationExtensions.GetBindedConfiguration<Settings>(null, _dir.Path, "appsettings.json");

        config.Root.ShouldBe("r");
        config.App!.Name.ShouldBe("demo");
        config.App.Port.ShouldBe(8080);
        config.App.Nested!.Flag.ShouldBeTrue();
    }

    [Fact]
    public void GetBindedConfiguration_BindsOnlyTheNamedSection()
    {
        var config = FExConfigurationExtensions.GetBindedConfiguration<AppSection>("App", _dir.Path, "appsettings.json");

        config.Name.ShouldBe("demo");
        config.Port.ShouldBe(8080);
    }

    [Fact]
    public void GetBindedConfiguration_ForAMissingSection_ReturnsDefaults()
    {
        var config = FExConfigurationExtensions.GetBindedConfiguration<AppSection>("Nope", _dir.Path, "appsettings.json");

        config.Name.ShouldBeNull();
        config.Port.ShouldBe(0);
    }

    [Fact]
    public void GetBindedConfiguration_ForAMissingFile_Throws()
    {
        Should.Throw<FileNotFoundException>(() =>
            FExConfigurationExtensions.GetBindedConfiguration<Settings>(null, _dir.Path, "missing.json"));
    }

    [Fact]
    public void GetBindedConfiguration_WithAnExplicitFileName_ReadsThatFile()
    {
        File.WriteAllText(_dir.File("custom.json"), """{ "Name": "custom", "Port": 1 }""");

        var config = FExConfigurationExtensions.GetBindedConfiguration<AppSection>(null, _dir.Path, "custom.json");

        config.Name.ShouldBe("custom");
        config.Port.ShouldBe(1);
    }

    [Fact]
    public void GetBindedConfiguration_WithBasePathAndSection_UsesTheDefaultFileName()
    {
        FExConfigurationExtensions.GetBindedConfiguration<AppSection>("Other", _dir.Path).Name.ShouldBe("other");
    }

    [Fact]
    public void GetBindedConfiguration_WithoutAnyArguments_ReadsAppsettingsJsonFromTheCurrentDirectory()
    {
        Directory.SetCurrentDirectory(_dir.Path);

        var config = FExConfigurationExtensions.GetBindedConfiguration<Settings>();

        config.Root.ShouldBe("r");
        config.App!.Name.ShouldBe("demo");
    }

    [Fact]
    public void GetBindedConfiguration_WithOnlyASection_ReadsAppsettingsJsonFromTheCurrentDirectory()
    {
        Directory.SetCurrentDirectory(_dir.Path);

        FExConfigurationExtensions.GetBindedConfiguration<AppSection>("App").Port.ShouldBe(8080);
    }

    #endregion

    #region BindJsonNet

    [Fact]
    public void BindJsonNet_Generic_BuildsTheObjectFromTheSection_IncludingNestedObjects()
    {
        var section = Section("App", ("App:Name", "demo"), ("App:Port", "8080"), ("App:Nested:Flag", "true"));

        var app = section.BindJsonNet<AppSection>();

        app.Name.ShouldBe("demo");
        app.Port.ShouldBe(8080);
        app.Nested!.Flag.ShouldBeTrue();
    }

    [Fact]
    public void BindJsonNet_Generic_TurnsIndexedKeysIntoAnArray()
    {
        var section = Section("App", ("App:Tags:0", "a"), ("App:Tags:1", "b"), ("App:Tags:2", "c"));

        var app = section.BindJsonNet<AppSection>();

        app.Tags.ShouldBe(["a", "b", "c"]);
    }

    [Fact]
    public void BindJsonNet_Generic_TurnsIndexedObjectsIntoAnArrayOfObjects()
    {
        var section = Section("App", ("App:Servers:0:Host", "h0"), ("App:Servers:1:Host", "h1"));

        var app = section.BindJsonNet<AppSection>();

        app.Servers!.Select(s => s.Host).ShouldBe(["h0", "h1"]);
    }

    [Fact]
    public void BindJsonNet_Generic_ForAnEmptySection_ReturnsAFreshInstance()
    {
        var app = Section("Nope", ("Other:Name", "x")).BindJsonNet<AppSection>();

        app.ShouldNotBeNull();
        app.Name.ShouldBeNull();
    }

    [Fact]
    public void BindJsonNet_Generic_AppliesTheJsonFunctionBeforeDeserializing()
    {
        var section = Section("App", ("App:Title", "demo"));

        var app = section.BindJsonNet<AppSection>(static json => json.Replace("Title", "Name", StringComparison.Ordinal));

        app.Name.ShouldBe("demo");
    }

    [Fact]
    public void BindJsonNet_Instance_PopulatesAnExistingObject_KeepingUntouchedValues()
    {
        var section = Section("App", ("App:Name", "demo"), ("App:Port", "8080"));
        AppSection target = new() { Name = "old", Port = 1, Tags = ["kept"] };

        section.BindJsonNet(target);

        target.Name.ShouldBe("demo");
        target.Port.ShouldBe(8080);
        target.Tags.ShouldBe(["kept"]);
    }

    [Fact]
    public void BindJsonNet_Instance_AppliesTheJsonFunction()
    {
        var section = Section("App", ("App:Title", "demo"));
        AppSection target = new();

        section.BindJsonNet(target, static json => json.Replace("Title", "Name", StringComparison.Ordinal));

        target.Name.ShouldBe("demo");
    }

    #endregion

    private sealed class Sample
    {
        public string? Name { get; set; }
        public string? Other { get; set; }
        public int Port { get; set; }
        public InnerSample? Inner { get; set; }

        public string? Compute() => null;
    }

    private sealed class InnerSample
    {
        public string? Value { get; set; }
        public int Number { get; set; }
    }

    private sealed class Settings
    {
        public string? Root { get; set; }
        public AppSection? App { get; set; }
    }

    private sealed class AppSection
    {
        public string? Name { get; set; }
        public int Port { get; set; }
        public NestedSection? Nested { get; set; }
        public string[]? Tags { get; set; }
        public ServerSection[]? Servers { get; set; }
    }

    private sealed class NestedSection
    {
        public bool Flag { get; set; }
    }

    private sealed class ServerSection
    {
        public string? Host { get; set; }
    }
}
