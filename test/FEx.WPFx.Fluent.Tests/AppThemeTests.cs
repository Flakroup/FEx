using FEx.WPFx.Fluent.Testing;
using FEx.WPFx.Fluent.Themes;
using FEx.WPFx.Fluent.Views;
using Microsoft.Win32;
using Shouldly;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

/// <summary>The dictionary swap behind the live light/dark change: exactly one token set merged, nothing else touched.</summary>
[Collection(WpfGlobal.Collection)]
public sealed class AppThemeTests
{
    /// <summary>An Application whose Exit can be raised without running a message loop.</summary>
    private sealed class ExitableApplication : Application
    {
        public void RaiseExit() => OnExit(null!); // the base only forwards the event; the argument is unused by the handlers
    }

    private static Color Bg(ResourceDictionary resources) => ((SolidColorBrush)resources["Bg"]).Color;

    private static int TokenDictionaries(ResourceDictionary resources) =>
        resources.MergedDictionaries.Count(d => d.Source is { } s && ThemeChoice.AllDictionaryPaths.Contains(s.OriginalString));

    [Fact]
    public void Apply_MergesTheRequestedTokens() =>
        Sta.Run(() =>
        {
            var resources = new ResourceDictionary();
            AppTheme.Apply(resources, ThemeVariant.Dark);

            TokenDictionaries(resources).ShouldBe(1);
            Bg(resources).ShouldBe((Color)ColorConverter.ConvertFromString("#14171C"));
        });

    [Fact]
    public void Apply_SwitchingVariant_ReplacesInsteadOfStacking() =>
        Sta.Run(() =>
        {
            var resources = new ResourceDictionary();
            AppTheme.Apply(resources, ThemeVariant.Dark);
            AppTheme.Apply(resources, ThemeVariant.Light);

            TokenDictionaries(resources).ShouldBe(1);
            Bg(resources).ShouldBe((Color)ColorConverter.ConvertFromString("#F4F6F9"));
        });

    [Fact]
    public void Apply_TheSameVariantTwice_StillOneDictionary() =>
        Sta.Run(() =>
        {
            var resources = new ResourceDictionary();
            AppTheme.Apply(resources, ThemeVariant.Dark);
            AppTheme.Apply(resources, ThemeVariant.Dark);

            TokenDictionaries(resources).ShouldBe(1);
        });

    [Fact]
    public void Apply_LeavesOtherMergedDictionariesAlone() =>
        Sta.Run(() =>
        {
            var resources = new ResourceDictionary();
            var other = new ResourceDictionary { ["Marker"] = "kept" };
            resources.MergedDictionaries.Add(other);

            AppTheme.Apply(resources, ThemeVariant.Dark);
            AppTheme.Apply(resources, ThemeVariant.HighContrast);

            resources.MergedDictionaries.ShouldContain(other);
            resources["Marker"].ShouldBe("kept");
        });

    [Fact]
    public void Apply_HighContrast_ResolvesToSystemColours() =>
        Sta.Run(() =>
        {
            var resources = new ResourceDictionary();
            AppTheme.Apply(resources, ThemeVariant.HighContrast);

            Bg(resources).ShouldBe(SystemColors.WindowColor);
            ((SolidColorBrush)resources["Ink"]).Color.ShouldBe(SystemColors.WindowTextColor);
        });

    [Fact]
    public void Apply_RejectsANullTarget() =>
        Should.Throw<ArgumentNullException>(() => AppTheme.Apply(null!, ThemeVariant.Light));

    [Fact]
    public void Follow_RejectsANullApplication() =>
        Should.Throw<ArgumentNullException>(() => AppTheme.Follow(null!, ThemePreference.System));

    [Fact]
    public void Follow_AndSelect_SwitchTokensAndFluentLiveWithoutARestart() =>
        Sta.Run(() =>
        {
            // The one Application of the test process: Application.Current is per AppDomain, so no other test may create one.
            var application = new ExitableApplication { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            try
            {
                AppTheme.Follow(application, ThemePreference.System);

                TokenDictionaries(application.Resources).ShouldBe(1);
                Bg(application.Resources).ShouldBe(Bg(Merged(AppTheme.Resolve(ThemePreference.System))));
                application.ThemeMode.ShouldBe(ThemeMode.System);

                // Under Windows high contrast a pinned mode is overridden on purpose, so this part has nothing to say there.
                if (SystemParameters.HighContrast)
                    return;

                // Elements that already exist and are already under a presentation source: the live-repaint question.
                // Measured: Application.Resources changes reach an element through the Windows of the application, so the
                // button also sits in one (never shown, so nothing lands on the desktop); under a bare HwndSource it stays put.
                var button = new Button { Content = "x" };
                _ = new Window { Content = button };
                button.Measure(new Size(200, 50));

                AppTheme.Select(application, ThemePreference.Dark);
                application.ThemeMode.ShouldBe(ThemeMode.Dark);
                Bg(application.Resources).ShouldBe(DarkBg);
                button.Measure(new Size(200, 50)); // a running app gets this from the render pass
                var darkForeground = ((SolidColorBrush)button.Foreground).Color;

                AppTheme.Select(application, ThemePreference.Light);
                application.ThemeMode.ShouldBe(ThemeMode.Light);
                Bg(application.Resources).ShouldBe(LightBg);
                TokenDictionaries(application.Resources).ShouldBe(1);
                button.Measure(new Size(200, 50));
                ((SolidColorBrush)button.Foreground).Color.ShouldNotBe(darkForeground, "Fluent did not repaint the open button");

                // Fluent's own default font would be Segoe UI Variable; the library's token must win for every control.
                AppTheme.Select(application, ThemePreference.Light);
                var plain = new Button { Content = "y" };
                var textBlock = new TextBlock { Text = "x" };
                var host = new Window { Content = new StackPanel { Children = { plain, textBlock } } };
                host.Content.ShouldNotBeNull();
                plain.Measure(new Size(200, 50));
                textBlock.Measure(new Size(200, 50));
                plain.FontFamily.Source.ShouldBe("Segoe UI");
                textBlock.FontFamily.Source.ShouldBe("Segoe UI");

                // A Windows change must re-resolve the PINNED choice, not fall back to the Windows variant: pin the one
                // the system is not on, so a handler that reads the system setting instead lands on the other tokens.
                var system = AppTheme.Resolve(ThemePreference.System);
                var (pinned, pinnedBg, pinnedMode) = system == ThemeVariant.Dark
                    ? (ThemePreference.Light, LightBg, ThemeMode.Light)
                    : (ThemePreference.Dark, DarkBg, ThemeMode.Dark);
                AppTheme.Select(application, pinned);
                AppTheme.PreferenceHandler(application)(application, new UserPreferenceChangedEventArgs(UserPreferenceCategory.General));
#pragma warning disable VSTHRD001 // a test thread has no JoinableTaskFactory; draining the queued swap is the point
                application.Dispatcher.Invoke(() => { }, DispatcherPriority.Background); // lets the queued swap run first
#pragma warning restore VSTHRD001
                Bg(application.Resources).ShouldBe(pinnedBg);

                // A category that cannot change the variant leaves the tokens alone.
                AppTheme.OnWindowsPreferenceChanged(application, UserPreferenceCategory.Window);
#pragma warning disable VSTHRD001
                application.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
#pragma warning restore VSTHRD001
                Bg(application.Resources).ShouldBe(pinnedBg);
                application.ThemeMode.ShouldBe(pinnedMode);

                // Follow must hook the system event (and unhook it when the application ends).
                UserPreferenceChangedEventHandler? subscribed = null, unsubscribed = null;
                AppTheme.Follow(application, ThemePreference.System, handler => subscribed = handler, handler => unsubscribed = handler);
                subscribed.ShouldNotBeNull();
                unsubscribed.ShouldBeNull();
                application.RaiseExit();
                unsubscribed.ShouldBeSameAs(subscribed);
            }
            finally
            {
                // Application.Current outlives Shutdown() in a process with no message loop, and its Fluent resources
                // belong to this thread: leave them behind and every later view build on another thread trips on them.
                application.ThemeMode = ThemeMode.None;
                application.Resources.MergedDictionaries.Clear();
                application.Shutdown();
            }
        });

    private static readonly Color DarkBg = (Color)ColorConverter.ConvertFromString("#14171C");
    private static readonly Color LightBg = (Color)ColorConverter.ConvertFromString("#F4F6F9");

    private static ResourceDictionary Merged(ThemeVariant variant)
    {
        var resources = new ResourceDictionary();
        AppTheme.Apply(resources, variant);
        return resources;
    }

    [Theory]
    [InlineData(ThemePreference.System, ThemeVariant.Light, "System")]
    [InlineData(ThemePreference.System, ThemeVariant.Dark, "System")]
    [InlineData(ThemePreference.Light, ThemeVariant.Light, "Light")]
    [InlineData(ThemePreference.Dark, ThemeVariant.Dark, "Dark")]
    [InlineData(ThemePreference.Light, ThemeVariant.HighContrast, "System")] // Fluent picks its high-contrast set only in System mode
    [InlineData(ThemePreference.Dark, ThemeVariant.HighContrast, "System")]
    [InlineData(ThemePreference.System, ThemeVariant.HighContrast, "System")]
    public void ModeFor_KeepsFluentInStepWithTheTokens(ThemePreference preference, ThemeVariant variant, string expected) =>
        AppTheme.ModeFor(preference, variant).Value.ShouldBe(expected);

    [Fact]
    public void Select_RejectsANullApplication() =>
        Should.Throw<ArgumentNullException>(() => AppTheme.Select(null!, ThemePreference.Dark));

    [Theory]
    [InlineData(UserPreferenceCategory.General, true)]
    [InlineData(UserPreferenceCategory.Color, true)]
    [InlineData(UserPreferenceCategory.VisualStyle, true)]
    [InlineData(UserPreferenceCategory.Accessibility, true)]
    [InlineData(UserPreferenceCategory.Window, false)]
    [InlineData(UserPreferenceCategory.Locale, false)]
    [InlineData(UserPreferenceCategory.Desktop, false)]
    public void AffectsTheme_AcceptsOnlyTheCategoriesThatCanChangeTheVariant(UserPreferenceCategory category, bool expected) =>
        AppTheme.AffectsTheme(category).ShouldBe(expected);
}
