using FEx.WPFx.Fluent.Themes;
using Microsoft.Win32;
using Shouldly;
using System;
using System.Linq;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

/// <summary>Pins which token set a Windows setting maps to. High contrast must win: a user who chose a palette gets it.</summary>
public sealed class ThemeChoiceTests
{
    [Theory]
    [InlineData(false, 1, ThemeVariant.Light)]
    [InlineData(false, 0, ThemeVariant.Dark)]
    [InlineData(false, null, ThemeVariant.Dark)] // what WPF's ThemeMode.System does with no value at all
    [InlineData(true, 1, ThemeVariant.HighContrast)]
    [InlineData(true, 0, ThemeVariant.HighContrast)]
    [InlineData(true, null, ThemeVariant.HighContrast)]
    public void Pick_MapsTheSystemSettingToAVariant(bool highContrast, int? lightTheme, ThemeVariant expected) =>
        ThemeChoice.Pick(highContrast, lightTheme).ShouldBe(expected);

    [Theory]
    // System defers to the Windows setting, exactly like the two-argument Pick.
    [InlineData(ThemePreference.System, false, 1, ThemeVariant.Light)]
    [InlineData(ThemePreference.System, false, 0, ThemeVariant.Dark)]
    [InlineData(ThemePreference.System, false, null, ThemeVariant.Dark)]
    // A pinned choice ignores the Windows setting, whichever way it points: this is the follow-or-ignore rule.
    [InlineData(ThemePreference.Light, false, 1, ThemeVariant.Light)]
    [InlineData(ThemePreference.Light, false, 0, ThemeVariant.Light)]
    [InlineData(ThemePreference.Light, false, null, ThemeVariant.Light)]
    [InlineData(ThemePreference.Dark, false, 1, ThemeVariant.Dark)]
    [InlineData(ThemePreference.Dark, false, 0, ThemeVariant.Dark)]
    [InlineData(ThemePreference.Dark, false, null, ThemeVariant.Dark)]
    // High contrast beats every choice.
    [InlineData(ThemePreference.System, true, 1, ThemeVariant.HighContrast)]
    [InlineData(ThemePreference.Light, true, 1, ThemeVariant.HighContrast)]
    [InlineData(ThemePreference.Light, true, 0, ThemeVariant.HighContrast)]
    [InlineData(ThemePreference.Dark, true, 1, ThemeVariant.HighContrast)]
    [InlineData(ThemePreference.Dark, true, null, ThemeVariant.HighContrast)]
    public void Pick_WithAPreference_PinsLightOrDark_AndLetsHighContrastWin(
        ThemePreference preference, bool highContrast, int? lightTheme, ThemeVariant expected) =>
        ThemeChoice.Pick(preference, highContrast, lightTheme).ShouldBe(expected);

    [Fact]
    public void Pick_CoversEveryPreferenceInTheMatrix() =>
        Enum.GetValues<ThemePreference>().Length.ShouldBe(3, "a new preference needs rows in the Pick matrix above");

    [Theory]
    [InlineData(null, ThemePreference.System)]
    [InlineData("", ThemePreference.System)]
    [InlineData("  ", ThemePreference.System)]
    [InlineData("System", ThemePreference.System)]
    [InlineData("Light", ThemePreference.Light)]
    [InlineData("LIGHT", ThemePreference.Light)]
    [InlineData("Dark", ThemePreference.Dark)]
    [InlineData(" dark ", ThemePreference.Dark)]
    [InlineData("HighContrast", ThemePreference.System)] // a variant, not a choice
    [InlineData("3", ThemePreference.System)] // parses as a number outside the enum
    [InlineData("Dark,Light", ThemePreference.System)] // Enum.TryParse would OR these as flags
    [InlineData("1", ThemePreference.System)] // Enum.TryParse would read this as Light
    [InlineData("2", ThemePreference.System)] // ... and this as Dark
    [InlineData("Dark,System", ThemePreference.System)] // the OR of these lands on Dark
    public void ParsePreference_AcceptsTheThreeNames_AndFallsBackToSystem(string? value, ThemePreference expected) =>
        ThemeChoice.ParsePreference(value).ShouldBe(expected);

    [Theory]
    [InlineData(ThemeVariant.Light, "/FEx.WPFx.Fluent;component/Themes/Tokens.xaml")]
    [InlineData(ThemeVariant.Dark, "/FEx.WPFx.Fluent;component/Themes/Tokens.Dark.xaml")]
    [InlineData(ThemeVariant.HighContrast, "/FEx.WPFx.Fluent;component/Themes/Tokens.HighContrast.xaml")]
    public void DictionaryPath_NamesTheFileOfEachVariant(ThemeVariant variant, string expected) =>
        ThemeChoice.DictionaryPath(variant).ShouldBe(expected);

    [Fact]
    public void AllDictionaryPaths_CoversEveryVariantExactlyOnce() =>
        ThemeChoice.AllDictionaryPaths.ShouldBe(
            [.. Enum.GetValues<ThemeVariant>().Select(ThemeChoice.DictionaryPath)], ignoreOrder: true);

    [Fact]
    public void ReadLightThemeSetting_FindsTheValueOnWindowsThatHasIt()
    {
        // A typo in the key path reads as "no value" everywhere, which Pick turns into a plausible variant; on a
        // machine whose own Personalize key holds the value, the default path must find it.
        using var personalize = Registry.CurrentUser.OpenSubKey(ThemeChoice.PersonalizeSubKey);
        Assert.SkipWhen(personalize?.GetValue("AppsUseLightTheme") is not int, "This account has no AppsUseLightTheme value.");

        ThemeChoice.ReadLightThemeSetting().ShouldBe((int)personalize!.GetValue("AppsUseLightTheme")!);
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(0, null, 0)]
    [InlineData(1, 0, 1)] // the app-mode value wins over the system one
    [InlineData(null, 1, 1)] // WPF's ThemeMode.System falls back to the system-mode value
    [InlineData(null, 0, 0)]
    [InlineData("junk", 1, 1)] // a value of the wrong type counts as absent
    [InlineData("junk", "junk", null)]
    public void ReadLightThemeSetting_PrefersTheAppValueThenTheSystemOne(object? apps, object? system, int? expected)
    {
        var subKey = $@"Software\FEx.WPFx.Fluent.Tests\Personalize-{Guid.NewGuid():N}";
        using (var key = Registry.CurrentUser.CreateSubKey(subKey))
        {
            if (apps is not null)
                key.SetValue("AppsUseLightTheme", apps);
            if (system is not null)
                key.SetValue("SystemUsesLightTheme", system);
        }

        try
        {
            ThemeChoice.ReadLightThemeSetting(subKey).ShouldBe(expected);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(subKey);
        }
    }

    [Fact]
    public void ReadLightThemeSetting_IsNullWhenTheKeyDoesNotExist() =>
        ThemeChoice.ReadLightThemeSetting($@"Software\FEx.WPFx.Fluent.Tests\Absent-{Guid.NewGuid():N}").ShouldBeNull();
}
