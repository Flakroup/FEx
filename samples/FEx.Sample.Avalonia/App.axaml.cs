using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FEx.Avaloniax;

namespace FEx.Sample.Avalonia;

public class App : FExAvaloniaApp<AppContainer>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override Window CreateMainWindow() => new MainWindow
    {
        DataContext = new MainWindowViewModel()
    };
}