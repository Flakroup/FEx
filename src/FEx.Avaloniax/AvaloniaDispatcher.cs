using Avalonia.Threading;
using FEx.Abstractions;
using FEx.Abstractions.Enums;
using FEx.Basics.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Avaloniax;

public class AvaloniaDispatcher : FExDispatcher
{
    protected static Dispatcher Dispatcher => Dispatcher.UIThread;

    public AvaloniaDispatcher(ILogger logger)
        : base(logger)
    {
    }

    public override bool CheckAccess(object sender = null) => Dispatcher.CheckAccess();

    public override void InvokeOnIdleMainThread(Action action, object sender = null) =>
        Dispatcher.Invoke(action, DispatcherPriority.ApplicationIdle);

    public override T InvokeOnIdleMainThread<T>(Func<T> action, object sender = null) =>
        Dispatcher.Invoke(action, DispatcherPriority.ApplicationIdle);

    public override void InvokeOnMainThread(Action action, object sender = null) => Dispatcher.Invoke(action);

    public override T InvokeOnMainThread<T>(Func<T> action, object sender = null) => Dispatcher.Invoke(action);

    public override async Task InvokeOnIdleMainThreadAsync(Action action, object sender = null) =>
        await Dispatcher.InvokeAsync(action, DispatcherPriority.ApplicationIdle);

    public override async Task<T> InvokeOnIdleMainThreadAsync<T>(Func<T> action, object sender = null) =>
        await Dispatcher.InvokeAsync(action, DispatcherPriority.ApplicationIdle);

    public override async Task InvokeOnMainThreadAsync(Action action, object sender = null) =>
        await Dispatcher.InvokeAsync(action);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<T> action, object sender = null) =>
        await Dispatcher.InvokeAsync(action);

    public override void BeginInvokeOnMainThread(Action action) =>
        FExFoundation.AsyncHelper.FireAndForget(() => Dispatcher.Invoke(action), AsyncMode.ThreadPool);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object sender = null) =>
        await Dispatcher.InvokeAsync(funcTask);

    public override async Task InvokeOnMainThreadAsync(Func<Task> funcTask, object sender = null) =>
        await Dispatcher.InvokeAsync(funcTask);

    public override void SendInThisOrMainThreadContext(Action action,
                                                       SynchronizationContext synchronizationContext = null,
                                                       uint timeout = 10000) =>
        Dispatcher.Invoke(action);
}