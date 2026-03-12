using FEx.WPFx.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace FEx.WPFx.Extensions;

public static class DependencyObjectExtensions
{
    public static TViewModel GetViewModel<TViewModel>(this FrameworkElement view) => view.InvokeOnDispatcherContext(() => (TViewModel)view.DataContext);

    public static void InvokeOnDispatcherContext(this DependencyObject sender,
                                                 Action action,
                                                 DispatcherPriority priority = DispatcherPriority.Send) => DispatcherService.InvokeOnDispatcherContext(action, sender, priority);

    public static async Task InvokeOnDispatcherContextAsync(this DependencyObject sender,
                                                            Action action,
                                                            DispatcherPriority priority = DispatcherPriority.Send) => await DispatcherService.InvokeOnDispatcherContextAsync(action, sender, priority);

    public static async Task<T> InvokeOnDispatcherContextAsync<T>(this DependencyObject sender,
                                                                  Func<T> action,
                                                                  DispatcherPriority priority =
                                                                      DispatcherPriority.Send) =>
        await DispatcherService.InvokeOnDispatcherContextAsync(action, sender, priority);

    public static T InvokeOnDispatcherContext<T>(this DependencyObject sender,
                                                 Func<T> action,
                                                 DispatcherPriority priority = DispatcherPriority.Send) =>
        DispatcherService.InvokeOnDispatcherContext(action, sender, priority);

    public static void DoUpdateSource(this DependencyProperty property, object source)
    {
        if (property is not null)
        {
            var elt = source as UIElement;

            if (elt is not null)
            {
                BindingExpression binding = BindingOperations.GetBindingExpression(elt, property);

                binding?.UpdateSource();
            }
        }
    }
}
