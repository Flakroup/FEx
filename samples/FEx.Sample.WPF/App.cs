using System;
using FEx.DependencyInjection.Abstractions;
using FEx.Samples.Shared;
using FEx.WPFx.Abstractions;
using System.Windows;
using System.Windows.Controls;

namespace FEx.Sample.WPF;

/// <summary>
/// Sample WPF application on the FEx bootstrapper. The container is built asynchronously (the demo database is
/// read by <see cref="DemoDbStartupModule" />), a startup window is shown meanwhile, and the main window is created by
/// <see cref="CreateMainWindow" /> once the container is ready. There is no App.xaml / StartupUri on purpose: the
/// bootstrapper rejects <c>StartupUri</c>, since WPF would load it before the container exists.
/// </summary>
public sealed class App : AppBootstrapper<AppContainer>
{
    [STAThread]
    public static void Main() => new App().Run();

    protected override void ComponentInitialize()
    {
    }

    protected override void OnActivation()
    {
    }

    protected override Window CreateStartupWindow() => new()
    {
        Title = "FEx Sample - WPF",
        Width = 360,
        Height = 120,
        WindowStartupLocation = WindowStartupLocation.CenterScreen,
        ResizeMode = ResizeMode.NoResize,
        Content = new TextBlock
        {
            Text = "Starting...",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        }
    };

    protected override Window CreateMainWindow() => new MainWindow
    {
        DataContext = new MainWindowViewModel(FExServiceProvider.Get<DemoDataStore>())
    };
}
