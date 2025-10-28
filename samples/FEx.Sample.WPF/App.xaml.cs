using FEx.DependencyInjection.Abstractions;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace FEx.Sample.WPF;

/// <summary>
/// Sample WPF application demonstrating StrongInject-only Multi-DI pattern.
/// Shows how to initialize FEx framework with StrongInject container.
/// NOTE: This is a minimal example. For full WPF functionality, see FEx.WPFx module documentation.
/// </summary>
[SuppressMessage("ReSharper", "RedundantExtendsListEntry")]
public sealed partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Initialize FEx with StrongInject container (no Microsoft DI needed for simple WPF)
            using var container = FExServiceProvider.Initialize<AppContainer>();

            // Create and show main window
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Initialization failed: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);
        }
    }
}