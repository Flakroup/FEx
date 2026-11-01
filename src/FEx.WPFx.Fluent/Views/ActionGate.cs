using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

namespace FEx.WPFx.Fluent.Views;

/// <summary>
/// What every action button of the tabs shares: an action whose prerequisites are not met is greyed out and says
/// why - in a tooltip that also shows on the disabled button, and in a "?" mark beside it. A click that slips through
/// anyway (the export folder vanished since the last check) is answered with a message box, never with a status
/// text that may already be on screen and so changes nothing the operator can see.
/// </summary>
public static class ActionGate
{
    private const string DatabaseMissingText = "Brak konfiguracji bazy - uzupełnij połączenie w Ustawieniach.";

    /// <summary>Why anything that reads the database cannot run, or null when a connection is configured.</summary>
    public static string? DatabaseMissing(bool hasDatabase) => hasDatabase ? null : DatabaseMissingText;

    /// <summary>The row to select for the operator when a list holds exactly one, otherwise null.</summary>
    public static T? Only<T>(IReadOnlyList<T> rows) where T : class => rows.Count == 1 ? rows[0] : null;

    /// <summary>
    /// Enables <paramref name="button"/> when nothing is missing and the action is not already running, and shows the
    /// reason otherwise. While running the button is simply disabled: the reason would be noise.
    /// </summary>
    public static void Apply(Button button, HintMark? hint, string? reason, bool busy = false)
    {
        Gate(button, reason, busy);
        if (hint is not null)
        {
            hint.Text = HintText(reason, busy);
        }
    }

    private static string HintText(string? reason, bool busy) => busy ? string.Empty : reason ?? string.Empty;

    private static void Gate(Button button, string? reason, bool busy)
    {
        button.IsEnabled = reason is null && !busy;
        button.ToolTip = busy ? null : reason;
        ToolTipService.SetShowOnDisabled(button, true);
    }

    /// <summary>The visible answer to a click that the guard refused.</summary>
    /// <param name="caption">The message box title - the application's name.</param>
    [ExcludeFromCodeCoverage] // ponytail: a modal MessageBox cannot run headless; a Notice seam is the upgrade if more call sites appear
    public static void Refuse(string reason, string caption) =>
        MessageBox.Show(reason, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
}
