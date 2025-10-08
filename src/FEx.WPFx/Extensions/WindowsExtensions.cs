using FEx.Agnostics.Abstractions.Extensions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Point = System.Windows.Point;

#if ISNETFULL
using FEx.WPFx.Natives;
#endif

namespace FEx.WPFx.Extensions;

public static class WindowsExtensions
{
    /// <summary>
    /// Gets the screen on which window is present.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <returns>The screen</returns>
    public static Screen GetWindowsScreen(this Window window) =>
        Screen.FromRectangle(new((int)window.Left, (int)window.Top, (int)window.Width, (int)window.Height));

    /// <summary>
    /// Centers the window on the screen.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="screen">The screen.</param>
    public static void CenterWindowOnTheScreen(this Window window, Screen screen)
    {
        Point f = window.GetDpiFactor();
        window.Left = screen.WorkingArea.Location.X * f.X + (screen.WorkingArea.Width * f.X - window.Width) / 2;
        window.Top = screen.WorkingArea.Location.Y * f.Y + (screen.WorkingArea.Height * f.Y - window.Height) / 2;
    }

    public static Point GetDpiFactor(this Visual control)
    {
        var source = PresentationSource.FromVisual(control);

        double dpiX = 96.0 * (source?.CompositionTarget?.TransformToDevice.M11 ?? 1);
        double dpiY = 96.0 * (source?.CompositionTarget?.TransformToDevice.M22 ?? 1);

        return new(96.0 / dpiX, 96.0 / dpiY);
    }

    /// <summary>
    /// Centers the window on top of the owner.
    /// </summary>
    /// <param name="window">The window.</param>
    public static void CenterWindowOnTopOfTheOwner(this Window window)
    {
        if (window.Owner is not null)
        {
            window.Left = window.Owner.Left + (window.Owner.Width - window.Width) / 2;
            window.Top = window.Owner.Top + (window.Owner.Height - window.Height) / 2;
        }
    }

    /// <summary>
    /// Places to primary monitor.
    /// </summary>
    /// <param name="window">The window.</param>
    public static void PlaceToPrimaryMonitor(this Window window)
    {
        Screen primaryScreen = Screen.AllScreens.FindInEnumerable(s => s.Primary);
        window.PlaceToMonitor(primaryScreen);
    }

    public static void PlaceToMonitor(this Window window, Screen screen)
    {
        if (screen is not null)
        {
            if (!window.IsLoaded)
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            window.CenterWindowOnTheScreen(screen);
        }
#if ISNETFULL
        window.BringOnTop();
#else
        window.Activate();
#endif
    }
}