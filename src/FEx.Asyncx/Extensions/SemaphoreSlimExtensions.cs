using FEx.Basics.Utilities;
using System;
using System.Threading;

namespace FEx.Asyncx.Extensions;

public static class SemaphoreSlimExtensions
{
    /// <summary>
    ///     Exits the <see cref="SemaphoreSlim" /> a specified number of times.
    /// </summary>
    /// <param name="semaphore">The semaphore.</param>
    /// <param name="releaseCount">The number of times to exit the semaphore.</param>
    /// <returns>
    ///     The previous count of the <see cref="SemaphoreSlim" />.
    /// </returns>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///     <paramref name="releaseCount" /> is less
    ///     than 1.
    /// </exception>
    /// <exception cref="T:System.Threading.SemaphoreFullException">
    ///     The <see cref="SemaphoreSlim" /> has
    ///     already reached its maximum size.
    /// </exception>
    /// <exception cref="T:System.ObjectDisposedException">
    ///     The current instance has already been
    ///     disposed.
    /// </exception>
    public static int SafeRelease(this SemaphoreSlim semaphore, int releaseCount = 1)
    {
        var count = 0;

        try
        {
            if (semaphore is FExSemaphoreSlim { IsDisposed: true })
                return count;

            count = semaphore?.CurrentCount is not null
                ? semaphore.CurrentCount - releaseCount
                : 0;

            return semaphore?.Release(releaseCount) ?? count;
        }
        catch (ObjectDisposedException)
        {
            //ignored
            return count;
        }
    }
}