using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FEx.Avaloniax;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Sample.Avalonia;

[SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
public partial class App : FExAvaloniaApp<AppContainer>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void AfterServicesContainerBuild()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
    }
}