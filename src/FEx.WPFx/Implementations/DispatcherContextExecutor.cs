using FEx.Core.Abstractions.Implementations;
using FEx.Core.Abstractions.Interfaces;
using FEx.WPFx.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FEx.WPFx.Implementations;

[SuppressMessage("ReSharper", "OptionalParameterHierarchyMismatch")]
public class DispatcherContextExecutor : FExDispatcher
{
    public DispatcherContextExecutor(ILogger<DispatcherContextExecutor> logger,
                                     IMainThreadContextProvider mainThreadContextProvider,
                                     IDeadlockMonitor deadlockMonitor,
                                     IStackTraceProvider stackTraceProvider)
        : base(logger, mainThreadContextProvider, deadlockMonitor, stackTraceProvider)
    {
    }

    public override bool CheckAccess(object? sender) => DispatcherService.CheckAccess(sender as DispatcherObject);

    public override void BeginInvokeOnMainThread(Action action, object? sender) => DispatcherService.BeginInvoke(action);

    public override void InvokeOnIdleMainThread(Action action, object? sender) =>
        DispatcherService.InvokeOnDispatcherContext(action,
            sender as DispatcherObject,
            DispatcherPriority.ApplicationIdle);

    public override T InvokeOnIdleMainThread<T>(Func<T> action, object? sender) =>
        DispatcherService.InvokeOnDispatcherContext(action,
            sender as DispatcherObject,
            DispatcherPriority.ApplicationIdle);

    public override void InvokeOnMainThread(Action action, object? sender) =>
        DispatcherService.InvokeOnDispatcherContext(action, sender as DispatcherObject);

    public override async Task<T> InvokeOnIdleMainThreadAsync<T>(Func<T> action, object? sender) =>
        await DispatcherService.InvokeOnDispatcherContextAsync(action,
            sender as DispatcherObject,
            DispatcherPriority.ApplicationIdle);

    public override async Task InvokeOnMainThreadAsync(Action action, object? sender) =>
        await DispatcherService.InvokeOnDispatcherContextAsync(action, sender as DispatcherObject);

    public override T InvokeOnMainThread<T>(Func<T> action, object? sender) =>
        DispatcherService.InvokeOnDispatcherContext(action, sender as DispatcherObject);

    public override async Task InvokeOnIdleMainThreadAsync(Action action, object? sender) =>
        await DispatcherService.InvokeOnDispatcherContextAsync(action,
            sender as DispatcherObject,
            DispatcherPriority.ApplicationIdle);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<T> action, object? sender) =>
        await DispatcherService.InvokeOnDispatcherContextAsync(action, sender as DispatcherObject);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object? sender) =>
        await DispatcherService.ExecuteTaskInDispatcherContextAsync(funcTask);

    public override async Task InvokeOnMainThreadAsync(Func<Task> funcTask, object? sender) =>
        await DispatcherService.ExecuteTaskInDispatcherContextAsync(funcTask);
}