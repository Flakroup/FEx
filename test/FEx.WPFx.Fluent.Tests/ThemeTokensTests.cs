using FEx.WPFx.Fluent.Testing;
using FEx.WPFx.Fluent.Themes;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

/// <summary>
/// Pins the colour tokens against the table the design was signed off with: every key, in light and
/// dark, exactly - a mistyped digit would otherwise ship as a slightly wrong grey nobody can name. The contrast
/// cases pin the claim the design rests on (WCAG AA, 4.5:1) so a later "tweak" cannot quietly break it.
/// </summary>
public sealed class ThemeTokensTests
{
    /// <summary>Token, light, dark - the signed-off design table, verbatim.</summary>
    private static readonly (string Key, string Light, string Dark)[] DesignTable =
    [
        ("Bg", "#F4F6F9", "#14171C"),
        ("Surface", "#FFFFFF", "#1D2128"),
        ("Ink", "#18202B", "#E6E9EE"),
        ("Muted", "#566070", "#A3ACB9"),
        ("Faint", "#8A93A2", "#7C8594"),
        ("Line", "#DCE1E8", "#2E343D"),
        ("Line2", "#EEF1F5", "#262B33"),
        ("CtrlLine", "#C9D0DA", "#3A414B"),
        ("InputLine", "#8C96A5", "#5A6370"),
        ("Accent", "#1D5BBF", "#7AA5F5"),
        ("OnAccent", "#FFFFFF", "#0B1220"),
        ("AccentSoft", "#E8F0FC", "#1F2E4A"),
        ("AccentInk", "#123F86", "#C4D7FF"),
        ("DangerButton", "#B3261E", "#F08A80"),
        ("OnDanger", "#FFFFFF", "#1A0A08"),
        ("Danger", "#B3261E", "#F4A39B"),
        ("DangerSoft", "#FCEBEA", "#3A1E1E"),
        ("WarnSoft", "#FFF3DC", "#3A2E14"),
        ("WarnInk", "#7A4B00", "#F2C46D"),
        ("WarnBar", "#C98A00", "#E0A93B"),
        ("Ok", "#17703F", "#7FD6A2"),
        ("OkSoft", "#E7F4EC", "#16301F"),
        ("DisabledBg", "#EEF1F5", "#262B33"),
        ("DisabledInk", "#6B7482", "#8A93A2"),
        ("HeadBg", "#F4F6F9", "#232830"),
        ("HeadInk", "#3A4352", "#C3CAD4"),
        ("RowLine", "#F0F2F5", "#262B33"),
        ("OverlayHead", "#18202B", "#0F1216"),
    ];

    /// <summary>Tokens the design addendum added for the lock overlays.</summary>
    private static readonly string[] OverlayExtras = ["OverlayScrim", "OnOverlayHead"];

    /// <summary>Text on background pairs that must reach 4.5:1. Faint and DisabledInk are exempt by design.</summary>
    private static readonly (string Text, string Background)[] TextPairs =
    [
        ("Ink", "Bg"), ("Ink", "Surface"), ("Muted", "Bg"), ("Muted", "Surface"),
        ("OnAccent", "Accent"), ("Accent", "Surface"), ("AccentInk", "AccentSoft"),
        ("OnDanger", "DangerButton"), ("Danger", "Surface"), ("Danger", "DangerSoft"),
        ("WarnInk", "WarnSoft"), ("WarnInk", "Surface"), ("Ok", "OkSoft"), ("Ok", "Surface"),
        ("HeadInk", "HeadBg"),
        ("OnOverlayHead", "OverlayHead"),
    ];

    private static Dictionary<string, Color> Load(ThemeVariant variant)
    {
        Dictionary<string, Color>? colours = null;
        Sta.Run(() =>
        {
            var dictionary = new ResourceDictionary { Source = new Uri(ThemeChoice.DictionaryPath(variant), UriKind.Relative) };
            colours = dictionary.Keys.Cast<object>().Where(key => dictionary[key] is SolidColorBrush).ToDictionary(
                key => (string)key, key => ((SolidColorBrush)dictionary[key]).Color);
        });
        return colours!;
    }

    private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    [Fact]
    public void Light_DefinesEveryDesignTokenWithTheDesignValue()
    {
        var light = Load(ThemeVariant.Light);
        foreach (var (key, expected, _) in DesignTable)
            light[key].ShouldBe(Parse(expected), $"light {key}");
    }

    [Fact]
    public void Dark_DefinesEveryDesignTokenWithTheDesignValue()
    {
        var dark = Load(ThemeVariant.Dark);
        foreach (var (key, _, expected) in DesignTable)
            dark[key].ShouldBe(Parse(expected), $"dark {key}");
    }

    [Theory]
    [InlineData(ThemeVariant.Light)]
    [InlineData(ThemeVariant.Dark)]
    [InlineData(ThemeVariant.HighContrast)]
    public void EveryVariant_DefinesTheSameKeys(ThemeVariant variant) =>
        Load(variant).Keys.ShouldBe(
            [.. DesignTable.Select(row => row.Key), .. OverlayExtras], ignoreOrder: true);

    [Theory]
    [InlineData(ThemeVariant.Light)]
    [InlineData(ThemeVariant.Dark)]
    [InlineData(ThemeVariant.HighContrast)]
    public void EveryVariant_CarriesTheFontToken(ThemeVariant variant) =>
        Sta.Run(() =>
        {
            var dictionary = new ResourceDictionary { Source = new Uri(ThemeChoice.DictionaryPath(variant), UriKind.Relative) };

            dictionary[ThemeChoice.FontFamilyKey].ShouldBeOfType<FontFamily>().Source.ShouldBe("Segoe UI");
        });

    [Fact]
    public void Light_And_Dark_AreNotTheSamePalette() =>
        // A dark file copied from the light one would pass every key check and still be a light UI.
        DesignTable.Count(row => row.Light != row.Dark).ShouldBeGreaterThan(DesignTable.Length - 4);

    [Theory]
    [InlineData(ThemeVariant.Light)]
    [InlineData(ThemeVariant.Dark)]
    public void TextPairs_ReachWcagAa(ThemeVariant variant)
    {
        var colours = Load(variant);
        foreach (var (text, background) in TextPairs)
        {
            Contrast(colours[text], colours[background])
                .ShouldBeGreaterThanOrEqualTo(4.5, $"{variant}: {text} on {background}");
        }
    }

    [Fact]
    public void Contrast_OfBlackOnWhite_Is21() =>
        Contrast(Colors.Black, Colors.White).ShouldBe(21.0, 1e-9);

    [Fact]
    public void Contrast_OfAColourOnItself_Is1() =>
        Contrast(Parse("#1D5BBF"), Parse("#1D5BBF")).ShouldBe(1.0, 1e-9);

    /// <summary>WCAG 2.x contrast ratio between two opaque colours.</summary>
    private static double Contrast(Color a, Color b)
    {
        var (lighter, darker) = (Luminance(a), Luminance(b)) is var (x, y) && x >= y ? (x, y) : (Luminance(b), Luminance(a));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double Luminance(Color c)
    {
        static double Channel(byte value)
        {
            var s = value / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }
}
