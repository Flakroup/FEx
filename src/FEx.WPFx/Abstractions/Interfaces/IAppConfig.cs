using FEx.MVVM.Abstractions;
using System.Windows.Controls;
using System.Windows.Media;

namespace FEx.WPFx.Abstractions.Interfaces;

public interface IAppConfig
{
    IViewDesign SplashDesign { get; }
    IViewDesign MainDesign { get; }
    string? SplashResourceName { get; }
    WidthAndHeight? SplashSize { get; }
    string? ApplicationLogoResourceName { get; }
    string? WindowIconName { get; }
    ImageSource? ApplicationLogo { get; }
    Image? WindowIcon { get; }
    string? ChangelogWindowHeader { get; }

    void Initialize();
}