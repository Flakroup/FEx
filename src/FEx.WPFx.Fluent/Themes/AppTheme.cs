using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;

namespace FEx.WPFx.Fluent.Themes;

/// <summary>
/// Keeps the colour tokens in step with the operator's choice (follow Windows, light, dark). Fluent's own controls
/// follow it through <c>Application.ThemeMode</c>; the views' tokens are plain brushes, so something has to swap
/// their dictionary - this does, at start, on a pick in Settings and whenever Windows reports a preference change.
/// </summary>
public static class AppTheme
{
    /// <summary>Swaps the token dictionary merged into <paramref name="target"/> for <paramref name="variant"/>'s.</summary>
    /// <remarks>Any other merged dictionary is left alone. A fresh instance is loaded each time, so the
    /// high-contrast set - which reads system colours once - is current after every call.</remarks>
    public static void Apply(ResourceDictionary target, ThemeVariant variant)
    {
        ArgumentNullException.ThrowIfNull(target);

        var merged = target.MergedDictionaries;
        for (var i = merged.Count - 1; i >= 0; i--)
        {
            if (IsTokenDictionary(merged[i]))
                merged.RemoveAt(i);
        }

        merged.Add(new ResourceDictionary { Source = new Uri(ThemeChoice.DictionaryPath(variant), UriKind.Relative) });
    }

    /// <summary>The variant <paramref name="preference"/> resolves to under the system settings right now.</summary>
    public static ThemeVariant Resolve(ThemePreference preference) =>
        ThemeChoice.Pick(preference, SystemParameters.HighContrast, ThemeChoice.ReadLightThemeSetting());

    /// <summary>
    /// What <c>Application.ThemeMode</c> must be so Fluent's own controls agree with the tokens: System when the
    /// operator follows Windows or high contrast is on (Fluent picks its high-contrast set only in that mode), the
    /// pinned Light or Dark otherwise.
    /// </summary>
    internal static ThemeMode ModeFor(ThemePreference preference, ThemeVariant variant) =>
        variant == ThemeVariant.HighContrast || preference == ThemePreference.System ? ThemeMode.System
        : preference == ThemePreference.Light ? ThemeMode.Light
        : ThemeMode.Dark;

    // The preference the preference-changed handler resolves against; Select replaces it.
    private static ThemePreference _preference = ThemePreference.System;

    /// <summary>
    /// Applies <paramref name="preference"/> to <paramref name="application"/> now and keeps it applied: a later
    /// Windows change re-resolves it (it only moves the variant for <see cref="ThemePreference.System"/> or high
    /// contrast), until the application exits. Call before the first window is created, so no frame is painted
    /// with a missing token.
    /// </summary>
    public static void Follow(Application application, ThemePreference preference)
    {
        ArgumentNullException.ThrowIfNull(application);

        Follow(application, preference, handler => SystemEvents.UserPreferenceChanged += handler,
            handler => SystemEvents.UserPreferenceChanged -= handler);
    }

    /// <summary><see cref="Follow(Application, ThemePreference)"/> with the system event's add and remove injected, so a test can see them called.</summary>
    internal static void Follow(Application application, ThemePreference preference,
        Action<UserPreferenceChangedEventHandler> subscribe, Action<UserPreferenceChangedEventHandler> unsubscribe)
    {
        Select(application, preference);

        var onChanged = PreferenceHandler(application);
        subscribe(onChanged);
        application.Exit += (_, _) => unsubscribe(onChanged);
    }

    /// <summary>What <see cref="Follow(Application, ThemePreference)"/> subscribes, apart so a test can raise it without changing a Windows setting.</summary>
    internal static UserPreferenceChangedEventHandler PreferenceHandler(Application application) =>
        (_, e) => OnWindowsPreferenceChanged(application, e.Category);

    /// <summary>
    /// Re-resolves the remembered preference when Windows reports a change that can alter the variant. SystemEvents
    /// raises on its own thread, so the swap hops to the UI thread before touching resources.
    /// </summary>
    internal static void OnWindowsPreferenceChanged(Application application, UserPreferenceCategory category)
    {
        if (!AffectsTheme(category))
            return;

#pragma warning disable VSTHRD001 // the app has no JoinableTaskFactory; the swap is fire-and-forget by design
        _ = application.Dispatcher.InvokeAsync(() => Refresh(application));
#pragma warning restore VSTHRD001
    }

    /// <summary>Switches to <paramref name="preference"/> at once - tokens and Fluent's mode - and remembers it for later Windows changes.</summary>
    public static void Select(Application application, ThemePreference preference)
    {
        ArgumentNullException.ThrowIfNull(application);

        _preference = preference;
        Refresh(application);
    }

    private static void Refresh(Application application)
    {
        var variant = Resolve(_preference);
        var mode = ModeFor(_preference, variant);
        if (application.ThemeMode != mode)
            application.ThemeMode = mode;

        Apply(application.Resources, variant);
    }

    /// <summary>Whether a Windows preference change can alter the variant: app mode, colours, high contrast.</summary>
    internal static bool AffectsTheme(UserPreferenceCategory category) =>
        category is UserPreferenceCategory.General or UserPreferenceCategory.Color
            or UserPreferenceCategory.VisualStyle or UserPreferenceCategory.Accessibility;

    private static bool IsTokenDictionary(ResourceDictionary dictionary) =>
        dictionary.Source is { } source
        && ThemeChoice.AllDictionaryPaths.Any(path =>
            source.OriginalString.Equals(path, StringComparison.OrdinalIgnoreCase));
}
