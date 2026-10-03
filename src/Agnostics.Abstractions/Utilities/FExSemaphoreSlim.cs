using System.Threading;

namespace FEx.Agnostics.Abstractions.Utilities;

/// <summary>A <see cref="System.Threading.SemaphoreSlim" /> that remembers its initial count and whether it has been disposed.</summary>
public class FExSemaphoreSlim : SemaphoreSlim
{
    /// <summary>Gets a value indicating whether the semaphore has been disposed.</summary>
    public bool IsDisposed { get; protected set; }
    /// <summary>Gets the initial count the semaphore was created with.</summary>
    public int InitialCount { get; }
    /// <summary>Gets a value indicating whether no one currently holds the semaphore, that is the current count equals the initial count.</summary>
    public bool IsIdle => InitialCount == CurrentCount;

    /// <summary>Initializes the semaphore.</summary>
    /// <param name="initialCount">The initial number of requests that can be granted concurrently.</param>
    /// <param name="maxCount">The maximum number of requests that can be granted concurrently.</param>
    public FExSemaphoreSlim(int initialCount = 1, int maxCount = 1)
        : base(initialCount, maxCount)
    {
        InitialCount = initialCount;
    }

    #region IDisposable
    /// <summary>Marks the semaphore as disposed and releases its resources.</summary>
    /// <param name="disposing"><c>true</c> to release managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
    #endregion
}