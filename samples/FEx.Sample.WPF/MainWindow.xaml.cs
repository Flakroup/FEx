using FEx.DependencyInjection.Abstractions;
using System.Windows;

namespace FEx.Sample.WPF;

/// <summary>
/// Sample main window demonstrating FEx service usage
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        statusText.Text = "FEx initialized with StrongInject";
    }

    private void TestButton_Click(object sender, RoutedEventArgs e)
    {
        // Update UI to show action
        statusText.Text = $"Action at {DateTime.Now:HH:mm:ss}";

        // Demonstrate service resolution
        var serviceCount = FExServiceProvider.Instance != null
            ? "✅ Services available"
            : "❌ No services";

        MessageBox.Show(
            $"FEx Multi-DI Sample\n\n"
            + $"Container: StrongInject\n"
            + $"Services: {serviceCount}\n"
            + $"Pattern: StrongInject-only (no Microsoft DI)\n\n"
            + $"Framework initialized successfully!",
            "FEx Sample",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}