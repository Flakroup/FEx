using FEx.Abstractions;
using FEx.Common.Extensions;
using FEx.DI.Abstractions;
using FEx.WPFx.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace FEx.WPFx;

public class FExWpfx : InitializeModule<IFExWpfxContainer>
{
    public static EventHandler<RoutedEventArgs> WindowLoaded;
    public static List<string> ExcludedWindows { get; }
    public static string CloseString { get; set; }
    public static string MinimizeString { get; set; }
    public static string RestoreString { get; set; }
    public static string MaximizeString { get; set; }
    public static string ShowSystemMenuString { get; set; }
    public static bool IsMainWindowInitialized { get; private set; }
    public static Splash Splash { get; private set; }
    public static IAppConfig AppConfig { get; private set; }

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

    /// <summary>
    /// Overrides formatting on UI.
    /// </summary>
    /// <param name="culture">
    /// The culture to use. If <c>null</c>,
    /// <see cref="System.Globalization.CultureInfo.CurrentCulture" /> is used.
    /// </param>
    public static void OverrideFormattingOnUI(CultureInfo culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
    }

    /// <inheritdoc />
    protected override void AddServices(IFExWpfxContainer container, IServiceCollection services) =>
        FExWpfxModule.AddServices(container, services);

    private static void WindowInitialized(object sender, RoutedEventArgs e)
    {
        if (IsMainWindowInitialized)
            return;

        IsMainWindowInitialized = true;
#pragma warning disable CS0618 // Type or member is obsolete
        FExFoundation.MainThreadContextProvider.SetMainThread();
#pragma warning restore CS0618 // Type or member is obsolete
        Splash.WaitForSplashAndClose();
    }
}