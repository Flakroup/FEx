using FEx.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FEx.WPFx;

public static class DispatcherService
{
    /// <summary>
    ///     Executes the action in dispatcher context
    ///     by checking if action should be invoked by dispatcher asynchronously, or directly, and running it.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="sender">The sender object in context of which action should be executed.</param>
    /// <param name="priority">The priority.</param>
    public static void ExecuteActionInDispatcherContext(Action action,
                                                        DispatcherObject sender = null,
                                                        DispatcherPriority priority = DispatcherPriority.Send)
    {
        DispatcherObject dispatcherObject = sender.GetDispatcherObject();

        if (CheckAccess(dispatcherObject))
            action();
        else
            dispatcherObject.Dispatcher.Invoke(action, priority);
    }

    /// <summary>
    ///     Executes the action in dispatcher context
    ///     by checking if action should be invoked by dispatcher asynchronously, or directly, and running it.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="sender">The sender object in context of which action should be executed.</param>
    /// <param name="priority">The priority.</param>
    public static async Task ExecuteActionInDispatcherContextAsync(Action action,
                                                                   DispatcherObject sender = null,
                                                                   DispatcherPriority priority =
                                                                       DispatcherPriority.Send)
    {
        DispatcherObject dispatcherObject = sender.GetDispatcherObject();

        if (CheckAccess(dispatcherObject))
            action();
        else
            await dispatcherObject.Dispatcher.InvokeAsync(action, priority);
    }

    /// <summary>
    ///     Executes the action in dispatcher context
    ///     by checking if action should be invoked by dispatcher, or directly, and running it.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="sender">The sender object in context of which action should be executed.</param>
    /// <param name="priority">The priority.</param>
    public static T ExecuteActionInDispatcherContext<T>(Func<T> action,
                                                        DispatcherObject sender = null,
                                                        DispatcherPriority priority = DispatcherPriority.Send)
    {
        DispatcherObject dispatcherObject = sender.GetDispatcherObject();

        return CheckAccess(dispatcherObject)
            ? action()
            : dispatcherObject.Dispatcher.Invoke(action, priority);
    }

    /// <summary>
    ///     Executes the action in dispatcher context
    ///     by checking if action should be invoked by dispatcher, or directly, and running it.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="sender">The sender object in context of which action should be executed.</param>
    /// <param name="priority">The priority.</param>
    public static async Task<T> ExecuteActionInDispatcherContextAsync<T>(
        Func<T> action,
        DispatcherObject sender = null,
        DispatcherPriority priority = DispatcherPriority.Send)
    {
        DispatcherObject dispatcherObject = sender.GetDispatcherObject();

        return CheckAccess(dispatcherObject)
            ? action()
            : await dispatcherObject.Dispatcher.InvokeAsync(action, priority);
    }

    /// <summary>
    ///     Shows the view and waits until it's closed.
    /// </summary>
    /// <param name="viewFunc">The view function.</param>
    /// <param name="isModal">if set to <c>true</c> [is modal].</param>
    public static TaskCompletionSource<bool> ShowView<T>(Func<T> viewFunc, bool isModal = false) where T : Window
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
    ///     Checks the access.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <returns>True if you're on the dispatcher thread, otherwise - false</returns>
    public static bool CheckAccess(DispatcherObject sender) =>
        sender.GetDispatcherObject().Dispatcher.CheckAccess();

    public static void BeginInvoke(Action action,
                                   DispatcherObject sender = null,
                                   DispatcherPriority priority = DispatcherPriority.Normal)
    {
        DispatcherObject dispatcherObject = sender.GetDispatcherObject();
        dispatcherObject.Dispatcher.BeginInvoke(action, priority);
    }

    private static void ShowView<T>(Func<T> viewFunc, bool isModal, TaskCompletionSource<bool> tcs) where T : Window
    {
        try
        {
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
            T view = viewFunc();
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