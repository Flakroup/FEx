using System;
using System.Threading.Tasks;

namespace FEx.WPFx.Fluent.Views;

/// <summary>Where a read-only tab's data is: not asked for yet, on its way, on screen, or why it is not.</summary>
public enum LoadState
{
    Idle,
    Loading,
    Loaded,
    Empty,
    Failed,
    Missing,
}

/// <summary>
/// The state machine behind a tab that loads its own data when it is opened. No WPF in here, so "activation loads
/// once, switching back does not, a parameter change does" is pinned by plain tests.
/// </summary>
public sealed class DataLoad<T>
{
    private readonly Func<bool> _configured;
    private readonly Func<string?> _connection;
    private string? _loadedFor;
    private readonly Func<DateTime> _now;
    private readonly Action<Exception> _onFailure;
    private int _generation;

    /// <param name="configured">Whether there is a data source to ask; false ends in <see cref="LoadState.Missing"/> without a query.</param>
    /// <param name="isEmpty">Whether a loaded result is "nothing to show", which has a state of its own.</param>
    /// <param name="onFailure">Where the exception goes (the app's log, its error report) - it never reaches the screen.</param>
    /// <param name="now">The clock behind <see cref="LoadedAt"/>.</param>
    /// <param name="connection">Identifies the database the data came from; data read from another one is stale.</param>
    public DataLoad(Func<bool> configured, Func<T, bool> isEmpty, Action<Exception> onFailure, Func<DateTime>? now = null,
        Func<string?>? connection = null)
    {
        _configured = configured;
        _connection = connection ?? (() => null);
        IsEmpty = isEmpty;
        _onFailure = onFailure;
        _now = now ?? (() => DateTime.Now);
    }

    private Func<T, bool> IsEmpty { get; }

    /// <summary>Runs the read off the UI thread; a test swaps in a runner that completes inline.</summary>
    public Func<Func<T>, Task<T>> Run { get; set; } = read => Task.Run(read);

    public LoadState State { get; private set; } = LoadState.Idle;

    public T? Data { get; private set; }

    public DateTime? LoadedAt { get; private set; }

    public Exception? Error { get; private set; }

    /// <summary>Raised after every state change, on the thread that called the load.</summary>
    public event Action? Changed;

    /// <summary>
    /// Whether opening the tab should ask for data. A tab with data keeps it; one that never loaded, or found no
    /// database configured, asks again (the operator may have filled the connection in on another tab). A failed
    /// one waits for the operator's refresh - a dead server would otherwise be retried on every tab switch. Data
    /// read from another database than the current one is never kept: acting on it would write to the wrong place.
    /// </summary>
    public bool WantsLoad =>
        State is LoadState.Idle or LoadState.Missing
        || State is not LoadState.Loading && _loadedFor != _connection();

    /// <summary>
    /// Reads the data now, whatever the state. A result that arrives after a newer load started is dropped, so a
    /// slow answer for the old parameter cannot overwrite the fast one for the new.
    /// </summary>
    /// <param name="plan">Called on the caller's thread, and only when there is a database to ask: it captures the
    /// parameters and resolves what the read needs, and returns the read that then runs off the UI thread.</param>
    public async Task ReloadAsync(Func<Func<T>> plan)
    {
        var mine = ++_generation;
        _loadedFor = _connection();
        Data = default;
        LoadedAt = null;
        Error = null;

        if (!_configured())
        {
            Move(LoadState.Missing);
            return;
        }

        T data;
        try
        {
            // Inside the try: a handler that throws on entering Loading must not leave the load stuck there.
            Move(LoadState.Loading);
            data = await Run(plan());
        }
        catch (Exception ex)
        {
            if (mine != _generation)
                return;

            if (_loadedFor != _connection())
            {
                // The failure was about the database the app no longer points at.
                await ReloadAsync(plan);
                return;
            }

            Error = ex;
            try
            {
                _onFailure(ex);
            }
            finally
            {
                Move(LoadState.Failed);
            }

            return;
        }

        if (mine != _generation)
            return;

        if (_loadedFor != _connection())
        {
            // Never publish rows read from the previous database.
            await ReloadAsync(plan);
            return;
        }

        Data = data;
        LoadedAt = _now();
        Move(IsEmpty(data) ? LoadState.Empty : LoadState.Loaded);
    }

    /// <summary>Hands an exception that surfaced outside the read (while showing its result) to the injected failure sink.</summary>
    internal void ReportFailure(Exception exception) => _onFailure(exception);

    private void Move(LoadState state)
    {
        State = state;
        Changed?.Invoke();
    }
}
