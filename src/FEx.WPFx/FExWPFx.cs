using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Core.Abstractions;
using FEx.WPFx.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace FEx.WPFx;

public class FExWpfx : FExInitializable
{
    public static EventHandler<RoutedEventArgs> WindowLoaded;
    public static List<string> ExcludedWindows { get; }
    public static string CloseString { get; set; }
    public static string MinimizeString { get; set; }
    public static string RestoreString { get; set; }
    public static string MaximizeString { get; set; }
    public static string ShowSystemMenuString { get; set; }
    public static bool IsMainWindowInitialized { get; private set; }
    // Set by the DI-constructed instance before any static access; guaranteed non-null at use.
    public static Splash Splash { get; private set; } = null!;

    // Set by the DI-constructed instance before any static access; guaranteed non-null at use.
    public static IAppConfig AppConfig { get; private set; } = null!;

    public FExWpfx(IAppConfig appConfig, Splash splash)
    {
        Splash = splash;
        AppConfig = appConfig.Guard(nameof(appConfig));
    }

    static FExWpfx()
    {
        CloseString = "Close";
        MinimizeString = "Minimize";
        RestoreString = "Restore";
        MaximizeString = "Maximize";
        ShowSystemMenuString = "Show System Menu";
        ExcludedWindows = [];
        WindowLoaded += WindowInitialized;
    }

    public static void OverrideFormattingOnUI() => OverrideFormattingOnUI(CultureInfo.CurrentCulture);

    /// <summary>
    /// Overrides formatting on UI.
    /// </summary>
    /// <param name="culture">
    /// The culture to use. If <c>null</c>,
    /// <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    public static void OverrideFormattingOnUI(CultureInfo culture)
    {
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
    }

    private static void WindowInitialized(object? sender, RoutedEventArgs e)
    {
        if (IsMainWindowInitialized)
            return;

        IsMainWindowInitialized = true;
#pragma warning disable CS0618 // Type or member is obsolete
        FExCoreStatics.MainThreadContextProvider.SetMainThread();
#pragma warning restore CS0618 // Type or member is obsolete
        Splash.WaitForSplashAndClose();
    }
}