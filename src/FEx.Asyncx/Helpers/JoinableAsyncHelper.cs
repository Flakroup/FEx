using FEx.Common.Extensions;
using FEx.Extensions.Collections.Dictionaries;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Asyncx.Helpers;

public static class JoinableAsyncHelper
{
    private static JoinableTaskFactoryHandler _mainJTF;
    private static ConcurrentDictionary<int, JoinableTaskFactoryHandler> Factories { get; } = new();

    private static JoinableTaskFactoryHandler MainJTF
    {
        get => _mainJTF.Guard();
        set => _mainJTF = value;
    }

    public static void SetMainJoinableTaskFactory(Thread mainThread) =>
        MainJTF = GetFactory(mainThread.Guard(nameof(mainThread)), true);

    public static async Task DelayWithoutDeadlockAsync(int millisecondsDelay,
                                                       CancellationToken cancellationToken = default) =>
        await AwaitWithoutDeadlockAsync(() => Task.Delay(millisecondsDelay, cancellationToken));

    public static void DelayWithoutDeadlock(int millisecondsDelay) =>
        AwaitWithoutDeadlock(() => Task.Delay(millisecondsDelay));

    public static async Task AwaitWithoutDeadlockAsync(Func<Task> func, bool onMainThread = false)
    {
        JoinableTaskFactoryHandler jtf = onMainThread
            ? MainJTF
            : GetFactory();

        await await jtf.RunAsync(func);
    }

    public static void AwaitWithoutDeadlock(Func<Task> func, bool onMainThread = false)
    {
        JoinableTaskFactoryHandler jtf = onMainThread
            ? MainJTF
            : GetFactory();

        jtf.Run(func);
    }

    public static async Task<T> AwaitWithoutDeadlockAsync<T>(Func<Task<T>> func, bool onMainThread = false)
    {
        JoinableTaskFactoryHandler jtf = onMainThread
            ? MainJTF
            : GetFactory();

        return await await jtf.RunAsync(func);
    }

    public static T AwaitWithoutDeadlock<T>(Func<Task<T>> func, bool onMainThread = false)
    {
        JoinableTaskFactoryHandler jtf = onMainThread
            ? MainJTF
            : GetFactory();

        return jtf.Run(func);
    }

    public static JoinableTaskFactoryHandler GetFactory(Thread thread = null, bool replace = false)
    {
        int key = thread?.ManagedThreadId ?? Environment.CurrentManagedThreadId;
        Func<JoinableTaskFactoryHandler> func = () => GetNew(thread);

        return replace
            ? Factories.AddOrUpdateValue(key, func)
            : Factories.GetOrAddValue(key, func);
    }

    private static JoinableTaskFactoryHandler GetNew(Thread thread)
    {
        int key = thread?.ManagedThreadId ?? Environment.CurrentManagedThreadId;

        SynchronizationContext syncCtx = thread is null
            ? SynchronizationContext.Current
            : thread.GetThreadSynchronizationContext();

        thread ??= Thread.CurrentThread;
#pragma warning disable IDISP001
        var owner = new JoinableTaskContext(thread, syncCtx);
#pragma warning restore IDISP001
        return new(key, new(owner));
    }
}