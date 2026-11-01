using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.WPFx.Fluent.Themes;

/// <summary>What the operator asked for in Settings: follow Windows, or pin light or dark.</summary>
public enum ThemePreference
{
    System,
    Light,
    Dark,
}

/// <summary>Which token set the views paint with.</summary>
public enum ThemeVariant
{
    Light,
    Dark,
    HighContrast,
}

/// <summary>
/// The pure decisions behind the theme: which variant a system setting maps to, and which dictionary file holds
/// it. Kept apart from <see cref="AppTheme"/> so the mapping is testable without a window or the registry.
/// </summary>
public static class ThemeChoice
{
    /// <summary>The key of the font token every variant defines (Segoe UI); bind a window's <c>FontFamily</c> to it.</summary>
    public const string FontFamilyKey = "ContentControlThemeFontFamily";

    internal const string PersonalizeSubKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";
    private const string SystemUsesLightThemeValue = "SystemUsesLightTheme";

    /// <summary>
    /// High contrast wins over everything: those users picked their own palette. Otherwise the light-theme value
    /// from <see cref="ReadLightThemeSetting"/> decides (0 = dark). A missing value means dark, because that is
    /// what WPF's own <c>ThemeMode.System</c> does (<c>ThemeManager.IsSystemThemeLight</c>) - the tokens must agree
    /// with the Fluent controls they sit under, or text turns white on a white panel.
    /// </summary>
    public static ThemeVariant Pick(bool highContrast, int? lightTheme) =>
        highContrast ? ThemeVariant.HighContrast
        : lightTheme is null or 0 ? ThemeVariant.Dark
        : ThemeVariant.Light;

    /// <summary>
    /// The variant for an operator's <paramref name="preference"/>: high contrast still wins over everything
    /// (accessibility beats taste), a pinned Light or Dark ignores the Windows setting, and System defers to
    /// <see cref="Pick(bool, int?)"/>.
    /// </summary>
    public static ThemeVariant Pick(ThemePreference preference, bool highContrast, int? lightTheme) =>
        highContrast ? ThemeVariant.HighContrast
        : preference switch
        {
            ThemePreference.Light => ThemeVariant.Light,
            ThemePreference.Dark => ThemeVariant.Dark,
            _ => Pick(false, lightTheme),
        };

    /// <summary>
    /// Parses the <c>Theme</c> value of the settings file. Only the three names count (any casing); a missing,
    /// blank or unknown value - a number, a comma list - means <see cref="ThemePreference.System"/>, so an older or
    /// hand-edited file keeps today's behaviour. <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> is not
    /// used: it reads "1" as Light and ORs "Dark,System" into Dark.
    /// </summary>
    public static ThemePreference ParsePreference(string? value) =>
        Enum.GetValues<ThemePreference>().FirstOrDefault(
            preference => string.Equals(preference.ToString(), value?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Relative component URI of the dictionary defining <paramref name="variant"/>'s tokens.</summary>
    public static string DictionaryPath(ThemeVariant variant) => variant switch
    {
        ThemeVariant.Dark => "/FEx.WPFx.Fluent;component/Themes/Tokens.Dark.xaml",
        ThemeVariant.HighContrast => "/FEx.WPFx.Fluent;component/Themes/Tokens.HighContrast.xaml",
        _ => "/FEx.WPFx.Fluent;component/Themes/Tokens.xaml",
    };

    /// <summary>Every dictionary path, so a swap can find whichever token set is currently merged.</summary>
    public static IReadOnlyList<string> AllDictionaryPaths { get; } =
        [.. Enum.GetValues<ThemeVariant>().Select(DictionaryPath)];

    /// <summary>
    /// Reads the app-mode value ("apps use light theme") and, when it is absent or not an int, the system-mode one -
    /// the same order WPF's <c>ThemeMode.System</c> reads them. Null when neither is usable.
    /// </summary>
    public static int? ReadLightThemeSetting(string subKey = PersonalizeSubKey)
    {
        using var key = Registry.CurrentUser.OpenSubKey(subKey);
        return key?.GetValue(AppsUseLightThemeValue) as int? ?? key?.GetValue(SystemUsesLightThemeValue) as int?;
    }
}
