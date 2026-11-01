using FEx.WPFx.Fluent.Shell;
using FEx.WPFx.Fluent.Testing;
using FEx.WPFx.Fluent.Themes;
using Shouldly;
using System;
using System.Windows;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

[Collection(WpfGlobal.Collection)]
public sealed class ShellPillsTests
{
    [Fact]
    public void Database_CoversTheThreeStatesOfTheDesign()
    {
        ShellPills.Database(configured: false, reachable: false).ShouldBe(new PillState(PillSeverity.Danger, "Brak konfiguracji bazy"));
        ShellPills.Database(configured: true, reachable: false).ShouldBe(new PillState(PillSeverity.Danger, "Brak połączenia z bazą"));
        ShellPills.Database(configured: true, reachable: true).ShouldBe(new PillState(PillSeverity.Ok, "Baza połączona"));
    }

    [Fact]
    public void Database_NotConfigured_NeverReadsAsConnected() =>
        // "Reachable" must not hide a missing configuration.
        ShellPills.Database(configured: false, reachable: true).Text.ShouldBe("Brak konfiguracji bazy");

    [Theory]
    [InlineData(PillSeverity.Danger, true)]
    [InlineData(PillSeverity.Warning, true)]
    [InlineData(PillSeverity.Ok, false)]
    public void DatabasePill_OpensSettings_OnlyWhileNotConnected(PillSeverity severity, bool opens) =>
        ShellPills.DatabaseOpensSettings(new PillState(severity, "x")).ShouldBe(opens);

    [Theory]
    [InlineData(PillSeverity.Ok, "OkSoft", "Ok", "Ok")]
    [InlineData(PillSeverity.Warning, "WarnSoft", "WarnInk", "WarnBar")]
    [InlineData(PillSeverity.Danger, "DangerSoft", "Danger", "Danger")]
    public void Severity_MapsToTheTokensOfTheTheme(PillSeverity severity, string background, string ink, string dot)
    {
        var state = new PillState(severity, "x");

        state.BackgroundKey.ShouldBe(background);
        state.InkKey.ShouldBe(ink);
        state.DotKey.ShouldBe(dot);
    }

    [Theory]
    [InlineData(ThemeVariant.Light)]
    [InlineData(ThemeVariant.Dark)]
    [InlineData(ThemeVariant.HighContrast)]
    public void EveryTokenKeyAPillCanAsk_ExistsInEveryThemeDictionary(ThemeVariant variant) =>
        Sta.Run(() =>
        {
            var tokens = new ResourceDictionary { Source = new Uri(ThemeChoice.DictionaryPath(variant), UriKind.Relative) };

            foreach (var severity in Enum.GetValues<PillSeverity>())
            {
                var state = new PillState(severity, "x");
                tokens.Contains(state.BackgroundKey).ShouldBeTrue($"{variant} {state.BackgroundKey}");
                tokens.Contains(state.InkKey).ShouldBeTrue($"{variant} {state.InkKey}");
                tokens.Contains(state.DotKey).ShouldBeTrue($"{variant} {state.DotKey}");
            }
        });
}
