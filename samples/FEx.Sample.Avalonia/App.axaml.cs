using Avalonia.Controls;
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

    protected override Window CreateMainWindow() => new MainWindow
    {
        DataContext = new MainWindowViewModel()
    };
}