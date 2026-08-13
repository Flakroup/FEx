using System.Diagnostics.CodeAnalysis;

namespace FEx.WPFx.Natives;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal enum ShowWindow
{
    /// <summary>
    /// Hide the window.
    /// </summary>
    SW_HIDE = 0,

    /// <summary>
    /// Show the window and activate it (as usual).
    /// </summary>
    SW_SHOWNORMAL = 1,

    /// <summary>
    /// Show the window minimized.
    /// </summary>
    SW_SHOWMINIMIZED = 2,

    /// <summary>
    /// Maximize the window.
    /// </summary>
    SW_MAXIMIZE = 3,

    /// <summary>
    /// Show the window in its most recent size and position but do not activate it.
    /// </summary>
    SW_SHOWNOACTIVATE = 4,

    /// <summary>
    /// Show the window.
    /// </summary>
    SW_SHOW = 5,

    /// <summary>
    /// Minimize the window.
    /// </summary>
    SW_MINIMIZE = 6,

    /// <summary>
    /// Show the window minimized but do not activate it.
    /// </summary>
    SW_SHOWMINNOACTIVE = 7,

    /// <summary>
    /// Show the window in its current state but do not activate it.
    /// </summary>
    SW_SHOWNA = 8,

    /// <summary>
    /// Restore the window (not maximized nor minimized).
    /// </summary>
    SW_RESTORE = 9
}