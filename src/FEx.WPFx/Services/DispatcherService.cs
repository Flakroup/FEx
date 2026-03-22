using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

#pragma warning disable VSTHRD001

namespace FEx.WPFx.Services;

public static class DispatcherService
{
    /// <summary>
    /// Executes the action in dispatcher context
    /// by checking if action should be invoked by dispatcher asynchronously, or directly, and running it.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="sender">The sender object in context of which action should be executed.</param>
    /// <param name="priority">The priority.</param>
    public static void InvokeOnDispatcherContext(Action action) =>
        InvokeOnDispatcherContext(action, null, DispatcherPriority.Send);

    public static void InvokeOnDispatcherContext(Action action, DispatcherObject sender) =>
        InvokeOnDispatcherContext(action, sender, DispatcherPriority.Send);

    public static void InvokeOnDispatcherContext(Action action,
                                                 DispatcherObject sender,
                                                 DispatcherPriority priority)
    {
        var dispatcherObject = sender.GetDispatcherObject();

        if (CheckAccess(dispatcherObject))
            action();
        else
            dispatcherObject.Dispatcher.Invoke(action, priority);
    }

    /// <summary>
    /// Executes the action in dispatcher context
    /// by checking if action should be invoked by dispatcher asynchronously, or directly, and running it.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="sender">The sender object in context of which action should be executed.</param>
    /// <param name="priority">The priority.</param>
    public static Task InvokeOnDispatcherContextAsync(Action action) =>
        InvokeOnDispatcherContextAsync(action, null, DispatcherPriority.Send);

    public static Task InvokeOnDispatcherContextAsync(Action action, DispatcherObject sender) =>
        InvokeOnDispatcherContextAsync(action, sender, DispatcherPriority.Send);

    public static async Task InvokeOnDispatcherContextAsync(Action action,
                                                            DispatcherObject sender,
                                                            DispatcherPriority priority)
    {
        var dispatcherObject = sender.GetDispatcherObject();

        if (CheckAccess(dispatcherObject))
            action();
        else
            await dispatcherObject.Dispatcher.InvokeAsync(action, priority);
    }

    /// <summary>
    /// Executes the action in dispatcher context
    /// by checking if action should be invoked by dispatcher, or directly, and running it.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="sender">The sender object in context of which action should be executed.</param>
    /// <param name="priority">The priority.</param>
    public static T InvokeOnDispatcherContext<T>(Func<T> action) =>
        InvokeOnDispatcherContext(action, null, DispatcherPriority.Send);

    public static T InvokeOnDispatcherContext<T>(Func<T> action, DispatcherObject sender) =>
        InvokeOnDispatcherContext(action, sender, DispatcherPriority.Send);

    public static T InvokeOnDispatcherContext<T>(Func<T> action,
                                                 DispatcherObject sender,
                                                 DispatcherPriority priority)
    {
        var dispatcherObject = sender.GetDispatcherObject();

        return CheckAccess(dispatcherObject)
            ? action()
            : dispatcherObject.Dispatcher.Invoke(action, priority);
    }

    /// <summary>
    /// Executes the action in dispatcher context
    /// by checking if action should be invoked by dispatcher, or directly, and running it.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="cancellationToken"></param>
    /// <param name="sender">The sender object in context of which action should be executed.</param>
    /// <param name="priority">The priority.</param>
    public static Task<T> InvokeOnDispatcherContextAsync<T>(Func<T> action) =>
        InvokeOnDispatcherContextAsync(action, null, DispatcherPriority.Send, default);

    public static Task<T> InvokeOnDispatcherContextAsync<T>(Func<T> action, DispatcherObject sender) =>
        InvokeOnDispatcherContextAsync(action, sender, DispatcherPriority.Send, default);

    public static Task<T> InvokeOnDispatcherContextAsync<T>(Func<T> action,
                                                            DispatcherObject sender,
                                                            DispatcherPriority priority) =>
        InvokeOnDispatcherContextAsync(action, sender, priority, default);

    public static async Task<T> InvokeOnDispatcherContextAsync<T>(Func<T> action,
                                                                  DispatcherObject sender,
                                                                  DispatcherPriority priority,
                                                                  CancellationToken cancellationToken)
    {
        var dispatcherObject = sender.GetDispatcherObject();

        return CheckAccess(dispatcherObject)
            ? action()
            : await dispatcherObject.Dispatcher.InvokeAsync(action, priority, cancellationToken);
    }

    public static Task ExecuteTaskInDispatcherContextAsync(Func<Task> funcTask) =>
        ExecuteTaskInDispatcherContextAsync(funcTask, null, DispatcherPriority.Send);

    public static Task ExecuteTaskInDispatcherContextAsync(Func<Task> funcTask, DispatcherObject sender) =>
        ExecuteTaskInDispatcherContextAsync(funcTask, sender, DispatcherPriority.Send);

    public static async Task ExecuteTaskInDispatcherContextAsync(Func<Task> funcTask,
                                                                 DispatcherObject sender,
                                                                 DispatcherPriority priority)
    {
        var dispatcherObject = sender.GetDispatcherObject();

        if (CheckAccess(dispatcherObject))
            await funcTask();
        else
            await dispatcherObject.Dispatcher.Invoke(funcTask, priority);
    }

    public static Task<T> ExecuteTaskInDispatcherContextAsync<T>(Func<Task<T>> funcTask) =>
        ExecuteTaskInDispatcherContextAsync(funcTask, null, DispatcherPriority.Send);

    public static Task<T> ExecuteTaskInDispatcherContextAsync<T>(Func<Task<T>> funcTask, DispatcherObject sender) =>
        ExecuteTaskInDispatcherContextAsync(funcTask, sender, DispatcherPriority.Send);

    public static async Task<T> ExecuteTaskInDispatcherContextAsync<T>(Func<Task<T>> funcTask,
                                                                       DispatcherObject sender,
                                                                       DispatcherPriority priority)
    {
        var dispatcherObject = sender.GetDispatcherObject();

        return CheckAccess(dispatcherObject)
            ? await funcTask()
            : await dispatcherObject.Dispatcher.Invoke(funcTask, priority);
    }

    /// <summary>
    /// Shows the view and waits until it's closed.
    /// </summary>
    /// <param name="viewFunc">The view function.</param>
    /// <param name="isModal">if set to <c>true</c> [is modal].</param>
    public static TaskCompletionSource<bool> ShowView<T>(Func<T> viewFunc) where T : Window =>
        ShowView(viewFunc, false);

    public static TaskCompletionSource<bool> ShowView<T>(Func<T> viewFunc, bool isModal) where T : Window
    {
        var tcs = new TaskCompletionSource<bool>();
        var thread = new Thread(() => ShowView(viewFunc, isModal, tcs));
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return tcs;
    }

    public static DispatcherObject GetDispatcherObject(this DispatcherObject sender) =>
        sender?.Dispatcher is not null
            ? sender
            : Application.Current;

    /// <summary>
    /// Checks the access.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <returns>True if you're on the dispatcher thread, otherwise - false</returns>
    public static bool CheckAccess(DispatcherObject sender) => sender.GetDispatcherObject().Dispatcher.CheckAccess();

    public static void BeginInvoke(Action action) =>
        BeginInvoke(action, null, DispatcherPriority.Normal);

    public static void BeginInvoke(Action action, DispatcherObject sender) =>
        BeginInvoke(action, sender, DispatcherPriority.Normal);

    public static void BeginInvoke(Action action,
                                   DispatcherObject sender,
                                   DispatcherPriority priority)
    {
        var dispatcherObject = sender.GetDispatcherObject();

        FExCoreStatics.AsyncHelper.FireTaskAndForget(async () =>
            await dispatcherObject.Dispatcher.BeginInvoke(action, priority));
    }

    private static void ShowView<T>(Func<T> viewFunc, bool isModal, TaskCompletionSource<bool> tcs) where T : Window
    {
        try
        {
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));

            var view = viewFunc();
            // When the window closes, shut down the dispatcher
            view.Closed += (_, _) => Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);

            view.Loaded += (_, _) =>
            {
                if (isModal)
                    tcs.SetResult(true);
            };

            if (isModal)
            {
                view.ShowDialog();
            }
            else
            {
                view.Show();
                tcs.SetResult(true);
                // Start the Dispatcher Processing
                Dispatcher.Run();
            }
        }
        catch (Exception ex)
        {
            ex.HandleException();
            tcs.SetResult(false);
        }
    }
}