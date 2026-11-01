using FEx.WPFx.Fluent.Shell;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace FEx.WPFx.Fluent.Views;

/// <summary>
/// Joins a <see cref="DataLoad{T}"/> to a tab: reads when the tab is first shown, again when its parameter changes
/// or the operator asks, and keeps the overlay and the freshness text in step. The tab supplies only what is its
/// own - how to read, how to show rows, how to show none. The view tells the shell around it through
/// <see cref="ShellLink"/>'s routed events; hosted without a shell it simply has nobody to tell.
/// </summary>
public sealed class ViewLoad<T>
{
    private readonly FrameworkElement _view;
    private readonly LoadOverlay _overlay;
    private readonly LoadIndicator _indicator;
    private readonly Func<Func<T>> _plan;
    private readonly Action<T> _show;
    private readonly Action _clear;
    private readonly Func<string> _emptyTitle;
    private readonly string? _emptyHint;

    /// <param name="view">The tab: its first show starts the read, and the shell's routed events bubble from it.</param>
    /// <param name="overlay">The overlay that shows the skeleton, the empty list and the errors.</param>
    /// <param name="indicator">The freshness text and the refresh button.</param>
    /// <param name="load">The load this tab drives.</param>
    /// <param name="plan">Called on the UI thread, only when there is a data source: captures the parameters, resolves the repository and returns the read that runs off it.</param>
    /// <param name="show">Puts a non-empty result on screen.</param>
    /// <param name="clear">Takes the rows off screen - before a load and when the result is empty or failed.</param>
    /// <param name="emptyTitle">The bold line of an empty list; read when it is shown, as it may name the chosen year.</param>
    /// <param name="emptyHint">The muted line under it; null where the tab has no parameter to change.</param>
    public ViewLoad(FrameworkElement view, LoadOverlay overlay, LoadIndicator indicator, DataLoad<T> load,
        Func<Func<T>> plan, Action<T> show, Action clear, Func<string> emptyTitle, string? emptyHint)
    {
        _view = view;
        _overlay = overlay;
        _indicator = indicator;
        Load = load;
        _plan = plan;
        _show = show;
        _clear = clear;
        _emptyTitle = emptyTitle;
        _emptyHint = emptyHint;

        load.Changed += Render;
        overlay.RetryRequested += (_, _) => Reload();
        overlay.OpenSettingsRequested += (_, _) => ShellLink.RaiseOpenSettings(view);
        indicator.RefreshRequested += (_, _) => Reload();
        view.IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is true && load.WantsLoad)
                Reload();
        };
        Render();
        if (view.IsVisible && load.WantsLoad)
            Reload();
    }

    public DataLoad<T> Load { get; }

    /// <summary>Reads again with the parameters as they are now; a parameter change and the refresh button both end here.</summary>
    public async Task ReloadAsync()
    {
        await Load.ReloadAsync(_plan);
    }

    /// <summary>A parameter changed: reloads, but only for a tab that has been opened - one that was never shown stays quiet.</summary>
    public void ParameterChanged()
    {
        if (Load.State != LoadState.Idle)
            Reload();
    }

    // The entry point of every UI trigger. Nothing awaits it, so whatever goes wrong while showing the result
    // goes to the load's failure sink inside the task.
    private void Reload() => _ = ReloadGuardedAsync();

    private async Task ReloadGuardedAsync()
    {
        try
        {
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            Load.ReportFailure(ex);
        }
    }

    private void Render()
    {
        var state = Load.State;
        var emptyTitle = string.Empty;
        try
        {
            if (state is LoadState.Loaded && Load.Data is { } data)
                _show(data);
            else
                _clear();

            if (state is LoadState.Empty)
                emptyTitle = _emptyTitle();
        }
        catch (Exception ex)
        {
            Load.ReportFailure(ex);
            if (state is LoadState.Loaded or LoadState.Empty)
            {
                // The read worked, so the database is not the problem: never send the operator to the connection.
                _overlay.ShowDisplayFailed();
                _indicator.Show(null);
                ShellLink.RaiseDatabaseReachability(_view, reachable: true);
                return;
            }
        }

        _overlay.Show(state, emptyTitle, _emptyHint);
        _indicator.Show(Load.LoadedAt);

        if (state is LoadState.Failed)
            ShellLink.RaiseDatabaseReachability(_view, reachable: false);
        else if (state is LoadState.Loaded or LoadState.Empty)
            ShellLink.RaiseDatabaseReachability(_view, reachable: true);
    }
}
