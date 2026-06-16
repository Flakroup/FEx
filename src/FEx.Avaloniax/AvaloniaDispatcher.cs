using Avalonia.Threading;
using FEx.Agnostics.Abstractions.Enums;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Implementations;
using FEx.Core.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace FEx.Avaloniax;

[SuppressMessage("ReSharper", "OptionalParameterHierarchyMismatch")]
public class AvaloniaDispatcher : FExDispatcher
{
    protected static Dispatcher Dispatcher => Dispatcher.UIThread;

    public AvaloniaDispatcher(ILogger logger,
                              IMainThreadContextProvider mainThreadContextProvider,
                              IDeadlockMonitor deadlockMonitor,
                              IStackTraceProvider stackTraceProvider)
        : base(logger, mainThreadContextProvider, deadlockMonitor, stackTraceProvider)
    {
    }

    public override bool CheckAccess(object sender) => Dispatcher.CheckAccess();

    public override void InvokeOnIdleMainThread(Action action, object sender) =>
        Dispatcher.Invoke(action, DispatcherPriority.ApplicationIdle);

    public override T InvokeOnIdleMainThread<T>(Func<T> action, object sender) =>
        Dispatcher.Invoke(action, DispatcherPriority.ApplicationIdle);

    public override void InvokeOnMainThread(Action action, object sender) => Dispatcher.Invoke(action);

    public override T InvokeOnMainThread<T>(Func<T> action, object sender) => Dispatcher.Invoke(action);

    public override async Task InvokeOnIdleMainThreadAsync(Action action, object sender) =>
        await Dispatcher.InvokeAsync(action, DispatcherPriority.ApplicationIdle);

    public override async Task<T> InvokeOnIdleMainThreadAsync<T>(Func<T> action, object sender) =>
        await Dispatcher.InvokeAsync(action, DispatcherPriority.ApplicationIdle);

    public override async Task InvokeOnMainThreadAsync(Action action, object sender) =>
        await Dispatcher.InvokeAsync(action);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<T> action, object sender) =>
        await Dispatcher.InvokeAsync(action);

    public override void BeginInvokeOnMainThread(Action action, object sender) =>
        FExCoreStatics.AsyncHelper.FireAndForget(() => Dispatcher.Invoke(action), AsyncMode.ThreadPool);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object sender) =>
        await Dispatcher.InvokeAsync(funcTask);

    public override async Task InvokeOnMainThreadAsync(Func<Task> funcTask, object sender) =>
        await Dispatcher.InvokeAsync(funcTask);

    public override void SendInContext(Action action, object sender, uint? timeout) => Dispatcher.Invoke(action);
}