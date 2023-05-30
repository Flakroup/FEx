using Microsoft.VisualStudio.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Async;

public static class JoinableAsyncHelper
{
    public static async Task DelayWithoutDeadlockAsync(int millisecondsDelay,
                                                       CancellationToken cancellationToken = default)
    {
        await AwaitWithoutDeadlockAsync(() => Task.Delay(millisecondsDelay, cancellationToken));
    }

    public static void DelayWithoutDeadlock(int millisecondsDelay)
    {
        AwaitWithoutDeadlock(() => Task.Delay(millisecondsDelay));
    }

    public static async Task AwaitWithoutDeadlockAsync(Func<Task> func)
    {
        var context = new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current);
        var jtf = new JoinableTaskFactory(context);
        await jtf.RunAsync(func);
    }

    public static void AwaitWithoutDeadlock(Func<Task> func)
    {
        var context = new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current);
        var jtf = new JoinableTaskFactory(context);
        jtf.Run(func);
    }

    public static async Task<T> AwaitWithoutDeadlockAsync<T>(Func<Task<T>> func)
    {
        var context = new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current);
        var jtf = new JoinableTaskFactory(context);
        return await jtf.RunAsync(func);
    }

    public static T AwaitWithoutDeadlock<T>(Func<Task<T>> func)
    {
        var context = new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current);
        var jtf = new JoinableTaskFactory(context);
        return jtf.Run(func);
    }
}