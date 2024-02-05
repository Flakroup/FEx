using System.Threading;

namespace FEx.Basics.Utilities;

public class FExSemaphoreSlim : SemaphoreSlim
{
    public int InitialCount { get; }
    public bool IsIdle => InitialCount == CurrentCount;

    public FExSemaphoreSlim(int initialCount = 1)
        : base(initialCount)
    {
        InitialCount = initialCount;
    }

    public FExSemaphoreSlim(int initialCount = 1, int maxCount = 1)
        : base(initialCount, maxCount)
    {
        InitialCount = initialCount;
    }
}