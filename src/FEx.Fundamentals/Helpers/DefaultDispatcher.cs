using FEx.Abstractions.Interfaces;
using FEx.Basics.Abstractions;
using FEx.Basics.Extensions;
using FEx.Common.Abstractions.Interfaces;
using FEx.Common.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Fundamentals.Helpers;

public class DefaultDispatcher : FExDispatcher
{
    public DefaultDispatcher(ILogger logger,
                             IMainThreadContextProvider mainThreadContextProvider,
                             IDeadlockMonitor deadlockMonitor,
                             IStackTraceProvider stackTraceProvider)
        : base(logger, mainThreadContextProvider, deadlockMonitor, stackTraceProvider)
    {
    }

    /// <summary>
    /// Returns true if you're on the UI thread
    /// </summary>
    /// <param name="sender"></param>
    /// <returns></returns>
    public override bool CheckAccess(object sender = null) =>
        sender is Thread originThread && originThread.Equals(Thread.CurrentThread);

    public override void BeginInvokeOnMainThread(Action action) => action();

    public override void InvokeOnIdleMainThread(Action action, object sender = null) =>
        //this implementation is not able to determine wherever UI context is idle
        InvokeOnMainThread(action, sender);

    public override T InvokeOnIdleMainThread<T>(Func<T> action, object sender = null) =>
        //this implementation is not able to determine wherever UI context is idle
        InvokeOnMainThread(action, sender);

    public override async Task<T> InvokeOnIdleMainThreadAsync<T>(Func<T> action, object sender = null) =>
        //this implementation is not able to determine wherever UI context is idle
        await InvokeOnMainThreadAsync(action, sender);

    public override async Task InvokeOnIdleMainThreadAsync(Action action, object sender = null) =>
        //this implementation is not able to determine wherever UI context is idle
        await InvokeOnMainThreadAsync(action, sender);

    public override void InvokeOnMainThread(Action action, object sender = null)
    {
        SynchronizationContext context = sender is Thread thread
            ? thread.GetThreadSynchronizationContext()
            : _mainThreadContextProvider.Context;

        if (CheckAccess(sender)
            || context is null)
            action();
        else
            context.SendInContext(sender, action);
    }

    public override async Task InvokeOnMainThreadAsync(Action action, object sender = null)
    {
        SynchronizationContext context = sender is Thread thread
            ? thread.GetThreadSynchronizationContext()
            : _mainThreadContextProvider.Context;

        if (CheckAccess(sender)
            || context is null)
            action();
        else
            await Task.Run(() => context.SendInContext(sender, action)); //todo wtf
    }

    public override T InvokeOnMainThread<T>(Func<T> action, object sender = null)
    {
        SynchronizationContext context = sender is Thread thread
            ? thread.GetThreadSynchronizationContext()
            : _mainThreadContextProvider.Context;

        return CheckAccess(sender) || context is null
            ? action()
            : context.SendInContext(sender, action);
    }

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<T> action, object sender = null)
    {
        SynchronizationContext context = sender is Thread thread
            ? thread.GetThreadSynchronizationContext()
            : _mainThreadContextProvider.Context;

        return CheckAccess(sender) || context is null
            ? action()
            : await Task.Run(() => context.SendInContext(sender, action));
    }

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object sender = null) =>
        await funcTask();

    public override async Task InvokeOnMainThreadAsync(Func<Task> funcTask, object sender = null) => await funcTask();
}