using System;

namespace FEx.WPFx.Fluent.Shell;

/// <summary>How the side menu is shown at the current window width.</summary>
public enum MenuMode
{
    /// <summary>248 DIP, icon and label, beside the content.</summary>
    Full,

    /// <summary>56 DIP icon rail beside the content; labels are tooltips.</summary>
    Rail,

    /// <summary>Not shown; the context bar's button opens it as a 280 DIP flyout over the content.</summary>
    Flyout,
}

/// <summary>The width thresholds and sizes of the menu modes, as pure functions.</summary>
public static class MenuLayout
{
    public const double FullFromWidth = 1200;
    public const double RailFromWidth = 900;

    public const double FullWidth = 248;
    public const double RailWidth = 56;
    public const double FlyoutWidth = 280;

    /// <summary>The mode the window width asks for before any manual collapse.</summary>
    public static MenuMode ForWidth(double clientWidth) =>
        clientWidth >= FullFromWidth ? MenuMode.Full
        : clientWidth >= RailFromWidth ? MenuMode.Rail
        : MenuMode.Flyout;

    /// <summary>
    /// Whether the context bar is cut down to the module name and status dots. That follows the width alone, not a
    /// manual collapse of the menu: collapsing the menu on a wide window leaves the bar as it was.
    /// </summary>
    public static bool IsCompactBar(double clientWidth) => clientWidth < FullFromWidth;

    /// <summary>Width of the menu panel itself in <paramref name="mode"/>.</summary>
    public static double PanelWidth(MenuMode mode) => mode switch
    {
        MenuMode.Full => FullWidth,
        MenuMode.Rail => RailWidth,
        _ => FlyoutWidth,
    };

    /// <summary>Space the menu takes from the content: nothing for the flyout, which floats over it.</summary>
    public static double ReservedWidth(MenuMode mode) => mode == MenuMode.Flyout ? 0 : PanelWidth(mode);

    /// <summary>The text of the toggle at the top of the menu.</summary>
    public static string ToggleLabel(MenuMode mode) => mode switch
    {
        MenuMode.Full => "Zwiń menu do ikon",
        MenuMode.Rail => "Rozwiń menu",
        _ => "Zamknij menu",
    };

    /// <summary>
    /// The icon of the toggle: the hamburger (Segoe Fluent Icons E700) in every mode. The label and the tooltip say
    /// whether a press folds, unfolds or closes; the glyph is the one the whole app already uses for "the menu".
    /// </summary>
    public static string ToggleGlyph(MenuMode mode) => "";
}

/// <summary>
/// The menu mode over time. A manual collapse or expand is kept until the window crosses a threshold - then the
/// width decides again - and is what gets stored in the settings, so it survives a restart.
/// </summary>
public sealed class MenuLayoutState
{
    private MenuMode? _manual;
    private MenuMode _automatic;

    /// <param name="width">Client width at start.</param>
    /// <param name="storedOverride">The manual state read from the settings (<see cref="Override"/>'s value), or blank.</param>
    public MenuLayoutState(double width, string? storedOverride)
    {
        _automatic = MenuLayout.ForWidth(width);
        _manual = Enum.TryParse<MenuMode>(storedOverride, out var stored) && stored is MenuMode.Full or MenuMode.Rail ? stored : null;
    }

    public MenuMode Mode => _automatic == MenuMode.Flyout ? MenuMode.Flyout : _manual ?? _automatic;

    /// <summary>The manual state to store, or an empty string when the width decides.</summary>
    public string Override => _manual?.ToString() ?? string.Empty;

    /// <summary>Feeds a new width. True when the effective mode changed.</summary>
    public bool Resize(double width)
    {
        var before = Mode;
        var automatic = MenuLayout.ForWidth(width);
        if (automatic != _automatic)
        {
            _automatic = automatic;
            _manual = null; // crossing a threshold hands the decision back to the width
        }

        return Mode != before;
    }

    /// <summary>Full -> rail, rail -> full. Does nothing in the flyout, whose toggle closes it instead.</summary>
    public void ToggleCollapse()
    {
        if (Mode == MenuMode.Flyout)
            return;

        _manual = Mode == MenuMode.Full ? MenuMode.Rail : MenuMode.Full;
    }
}
