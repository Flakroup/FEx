using System.Windows.Media;

namespace FEx.WPFx.Abstractions.Interfaces;

public interface IViewDesign
{
    double FontSize { get; set; }
    FontFamily FontFamily { get; set; }
    Brush Background { get; set; }
    Brush ControlBackground { get; set; }
    Brush Foreground { get; set; }
    Brush HeaderBackground { get; set; }
    Brush BorderBackground { get; set; }

    void Initialize();
}