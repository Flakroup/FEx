using FEx.AppSettings.Abstractions.Interfaces;
using FEx.AppSettings.ConfigurationEx;
using FEx.AppSettings.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace FEx.AppSettings.Tests;

/// <summary>The configuration service: legacy app.config settings plus any extra sources, read as typed values.</summary>
[Collection(ProcessStateCollection.Name)]
public sealed class ConfigurationServiceTests
{
    private static MemoryConfigurationSource Memory(params (string Key, string Value)[] values)
    {
        var data = new Dictionary<string, string?>();

        foreach (var (key, value) in values)
            data[key] = value;

        return new() { InitialData = data };
    }

    [Fact]
    public void BeforeBuild_NothingIsLoaded()
    {
        ConfigurationService service = new();

        service.Configuration.ShouldBeNull();
        service.AppSettings.ShouldBeNull();
    }

    [Fact]
    public void Build_ReadsTheLegacyAppConfigSettings()
    {
        ConfigurationService service = new();

        service.Build(null);

        service.AppSettings.ShouldNotBeNull();
        service.AppSettings["Legacy.Name"].ShouldBe("from-app-config");
        service.AppSettings["Legacy.Port"].ShouldBe("8080");
    }

    [Fact]
    public void Build_ExposesLegacyConnectionStringsUnderTheConnectionStringsSection()
    {
        ConfigurationService service = new();

        service.Build(null);

        service.Configuration.ShouldNotBeNull();
        service.Configuration["ConnectionStrings:Main"].ShouldBe("Server=legacy;Database=main");
        service.Configuration.GetConnectionString("Main").ShouldBe("Server=legacy;Database=main");
    }

    [Fact]
    public void Build_AddsExtraSources_AndALaterSourceWinsOverTheLegacyOne()
    {
        ConfigurationService service = new();

        service.Build([Memory(("Legacy.Name", "overridden"), ("Extra.Key", "extra"))]);

        service.AppSettings!["Legacy.Name"].ShouldBe("overridden");
        service.AppSettings["Extra.Key"].ShouldBe("extra");
        service.AppSettings["Legacy.Port"].ShouldBe("8080");
    }

    [Fact]
    public void Build_LeavesOutSectionsThatHaveNoValueOfTheirOwn()
    {
        ConfigurationService service = new();

        service.Build([Memory(("Section:Child", "x"))]);

        service.AppSettings.ShouldNotBeNull();
        service.AppSettings.ShouldNotContainKey("Section");
        service.AppSettings.ShouldNotContainKey("ConnectionStrings");
        service.Configuration!["Section:Child"].ShouldBe("x");
    }

    [Fact]
    public void Build_Twice_Throws_AndKeepsTheFirstConfiguration()
    {
        ConfigurationService service = new();
        service.Build([Memory(("First", "1"))]);
        var first = service.Configuration;

        var ex = Should.Throw<InvalidOperationException>(() => service.Build([Memory(("Second", "2"))]));

        ex.Message.ShouldBe("Configuration is already built");
        service.Configuration.ShouldBeSameAs(first);
        service.AppSettings!.ShouldNotContainKey("Second");
    }

    [Fact]
    public void GetSetting_ConvertsTheRawValueWithTheGivenFunction()
    {
        ConfigurationService service = new();
        service.Build(null);

        service.GetSetting("Legacy.Port", int.Parse).ShouldBe(8080);
        service.GetSetting("Legacy.Name", static s => s.ToUpperInvariant()).ShouldBe("FROM-APP-CONFIG");
    }

    [Fact]
    public void GetSetting_BuildsLazilyWhenNothingWasBuiltYet()
    {
        ConfigurationService service = new();

        service.GetSetting("Legacy.Name", static s => s).ShouldBe("from-app-config");
        service.Configuration.ShouldNotBeNull();
    }

    [Fact]
    public void GetSetting_ForAMissingKey_Throws()
    {
        ConfigurationService service = new();
        service.Build(null);

        Should.Throw<KeyNotFoundException>(() => service.GetSetting("No.Such.Key", static s => s));
    }

    [Theory]
    [InlineData("Legacy.Enabled", true)]
    [InlineData("Legacy.Disabled", false)]
    public void GetBoolSetting_ParsesTrueAndFalseIgnoringCase(string key, bool expected)
    {
        ConfigurationService service = new();
        service.Build(null);

        service.GetBoolSetting(key, null).ShouldBe(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void GetBoolSetting_ForAMissingKey_ReturnsTheDefault(bool? defaultValue)
    {
        ConfigurationService service = new();
        service.Build(null);

        service.GetBoolSetting("No.Such.Key", defaultValue).ShouldBe(defaultValue);
    }

    [Fact]
    public void GetBoolSetting_ForANonBooleanValue_Throws()
    {
        ConfigurationService service = new();
        service.Build(null);

        var ex = Should.Throw<InvalidOperationException>(() => service.GetBoolSetting("Legacy.Broken", null));

        ex.Message.ShouldBe("No conversion to bool from maybe string was provided");
    }

    [Fact]
    public void GetBoolSetting_BuildsLazilyWhenNothingWasBuiltYet()
    {
        ConfigurationService service = new();

        service.GetBoolSetting("Legacy.Enabled", null).ShouldBe(true);
        service.Configuration.ShouldNotBeNull();
    }

    [Fact]
    public void ExtensionBuild_BuildsWithoutExtraSources()
    {
        IConfigurationService service = new ConfigurationService();

        service.Build();

        service.Configuration.ShouldNotBeNull();
        service.AppSettings!["Legacy.Name"].ShouldBe("from-app-config");
    }

    [Fact]
    public void ExtensionGetBoolSetting_HasNoDefault_SoAMissingKeyIsNull()
    {
        IConfigurationService service = new ConfigurationService();
        service.Build();

        service.GetBoolSetting("Legacy.Enabled").ShouldBe(true);
        service.GetBoolSetting("No.Such.Key").ShouldBeNull();
    }

    [Fact]
    public void LegacyConfigurationProvider_IsItsOwnSource_AndLoadsAppSettingsAndConnectionStrings()
    {
        LegacyConfigurationProvider provider = new();

        provider.Build(new ConfigurationBuilder()).ShouldBeSameAs(provider);

        provider.Load();

        provider.TryGet("Legacy.Name", out var name).ShouldBeTrue();
        name.ShouldBe("from-app-config");
        provider.TryGet("ConnectionStrings:Main", out var connection).ShouldBeTrue();
        connection.ShouldBe("Server=legacy;Database=main");
        provider.TryGet("Nope", out _).ShouldBeFalse();
    }

    [Fact]
    public void LegacyConfigurationProvider_SkipsAnAppSettingWithoutAKey()
    {
        // The legacy collection rejects Add (read only) but takes Set, which is how a keyless entry gets in.
        System.Configuration.ConfigurationManager.AppSettings.Set(null, "keyless");

        try
        {
            LegacyConfigurationProvider provider = new();

            provider.Load();

            provider.TryGet("Legacy.Name", out var name).ShouldBeTrue();
            name.ShouldBe("from-app-config");
        }
        finally
        {
            System.Configuration.ConfigurationManager.AppSettings.Set(null, null);
        }
    }

    [Fact]
    public void MergeAppSettings_AddsMissingAppSettingsFromTheLibraryConfig_WithoutOverridingExistingEntries()
    {
        ConfigurationManagerExtensions.MergeAppSettings();

        System.Configuration.ConfigurationManager.AppSettings["Merged.Only"].ShouldBe("from-library-config");
        System.Configuration.ConfigurationManager.AppSettings["Legacy.Name"].ShouldBe("from-app-config");
        System.Configuration.ConfigurationManager.ConnectionStrings["Main"].ConnectionString
            .ShouldBe("Server=legacy;Database=main");
    }

    [Fact]
    public void MergeAppSettings_WhenTheLibraryConfigIsBroken_LeavesTheSettingsAloneAndDoesNotThrow()
    {
        WithLibraryConfig("<configuration><appSettings><add value=\"no-key\" /></appSettings></configuration>", () =>
        {
            Should.NotThrow(ConfigurationManagerExtensions.MergeAppSettings);

            System.Configuration.ConfigurationManager.AppSettings["no-key"].ShouldBeNull();
            System.Configuration.ConfigurationManager.AppSettings["Legacy.Name"].ShouldBe("from-app-config");
        });
    }

    [Fact]
    public void MergeAppSettings_WhenTheLibraryConfigHoldsNothingNew_ChangesNothing()
    {
        WithLibraryConfig("""
            <configuration>
              <appSettings><add key="Legacy.Name" value="ignored" /></appSettings>
              <connectionStrings><add name="Main" connectionString="ignored" /></connectionStrings>
            </configuration>
            """,
            () =>
            {
                Should.NotThrow(ConfigurationManagerExtensions.MergeAppSettings);

                System.Configuration.ConfigurationManager.AppSettings["Legacy.Name"].ShouldBe("from-app-config");
                System.Configuration.ConfigurationManager.ConnectionStrings["Main"].ConnectionString
                    .ShouldBe("Server=legacy;Database=main");
            });
    }

    // MergeAppSettings reads <FEx.AppSettings.dll>.config next to the library; swap it for one test, always restore it.
    private static void WithLibraryConfig(string xml, Action test)
    {
        var path = typeof(FExAppSettings).Assembly.Location + ".config";
        var original = File.ReadAllText(path);

        try
        {
            File.WriteAllText(path, xml);
            test();
        }
        finally
        {
            File.WriteAllText(path, original);
        }
    }
}
