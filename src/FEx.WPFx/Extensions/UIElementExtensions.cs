using FEx.Core.Abstractions;
using System;
using System.Windows;

namespace FEx.WPFx.Extensions;

public static class UIElementExtensions
{
    public static void ExecuteActionOnUIElement<T>(this T element, Action action, bool inUIContext = false)
        where T : UIElement
    {
        if (element is null
            || action is null)
            return;

        if (!inUIContext)
            action();
        else
            FExCoreStatics.Dispatcher.InvokeOnIdleMainThread(action);
    }

    public static void EnableUIElement(this UIElement element, bool inUIContext = false) =>
        element?.ExecuteActionOnUIElement(() => element.IsEnabled = true, inUIContext);

    public static void DisableUIElement(this UIElement element, bool inUIContext = false) =>
        element?.ExecuteActionOnUIElement(() => element.IsEnabled = false, inUIContext);
}
