namespace FEx.Agnostics.Abstractions.Enums;

/// <summary>Selects the thread context on which asynchronous work is run.</summary>
public enum AsyncMode
{
    /// <summary>Run on the default scheduler via <see cref="System.Threading.Tasks.Task.Run(System.Action)" />.</summary>
    Default,
    /// <summary>Run on the application's main (UI) thread.</summary>
    MainThread,
    /// <summary>Run on a thread-pool thread.</summary>
    ThreadPool
}