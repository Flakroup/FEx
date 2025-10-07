using System.Threading;

namespace FEx.Basics.Utilities;

public class FExSemaphoreSlim : SemaphoreSlim
{
    public bool IsDisposed { get; protected set; }
    public int InitialCount { get; }
    public bool IsIdle => InitialCount == CurrentCount;

    public FExSemaphoreSlim(int initialCount = 1, int maxCount = 1)
        : base(initialCount, maxCount)
    {
        InitialCount = initialCount;
    }

    #region IDisposable
    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
    #endregion
}