using FEx.Extensions;
using FEx.Extensions.Collections.Dictionaries;
using FEx.Fundamentals;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Asyncx.Helpers;

public static class JoinableAsyncHelper
{
    private static JoinableTaskFactory _mainJTF;
    private static ConcurrentDictionary<int, JoinableTaskFactory> Factories { get; } = new();

    private static JoinableTaskFactory MainJTF
    {
        get => _mainJTF.Guard();
        set => _mainJTF = value;
    }

    public static void SetMainJoinableTaskFactory(Thread mainThread = null)
    {
        MainJTF = GetFactory(mainThread ?? Foundation.MainThread);
    }

    public static async Task DelayWithoutDeadlockAsync(int millisecondsDelay,
                                                       CancellationToken cancellationToken = default)
    {
        await AwaitWithoutDeadlockAsync(() => Task.Delay(millisecondsDelay, cancellationToken));
    }

    public static void DelayWithoutDeadlock(int millisecondsDelay)
    {
        AwaitWithoutDeadlock(() => Task.Delay(millisecondsDelay));
    }

    public static async Task AwaitWithoutDeadlockAsync(Func<Task> func, bool onMainThread = false)
    {
        JoinableTaskFactory jtf = onMainThread
            ? MainJTF
            : GetFactory();

        await jtf.RunAsync(func);
    }

    public static void AwaitWithoutDeadlock(Func<Task> func, bool onMainThread = false)
    {
        JoinableTaskFactory jtf = onMainThread
            ? MainJTF
            : GetFactory();

        jtf.Run(func);
    }

    public static async Task<T> AwaitWithoutDeadlockAsync<T>(Func<Task<T>> func) => await GetFactory().RunAsync(func);

    public static T AwaitWithoutDeadlock<T>(Func<Task<T>> func) => GetFactory().Run(func);

    private static JoinableTaskFactory GetFactory(Thread thread = null) =>
        Factories.GetOrAddValue(thread?.ManagedThreadId ?? Environment.CurrentManagedThreadId, () => GetNew(thread));

    private static JoinableTaskFactory GetNew(Thread thread = null)
    {
        SynchronizationContext syncCtx = thread is null
            ? SynchronizationContext.Current
            : thread.GetThreadSynchronizationContext();

        thread ??= Thread.CurrentThread;
#pragma warning disable IDISP001
        var owner = new JoinableTaskContext(thread, syncCtx);
#pragma warning restore IDISP001
        return new JoinableTaskFactory(owner);
    }
}