using FEx.MVVM.Abstractions.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.MVVM.Abstractions;

public static class UIContextExtensions
{
    public static void InvokeOnMainThread(this IHasUIContext hasUIContext, Action action) =>
        hasUIContext.UIContextHandler.InvokeOnMainThread(action);

    public static T InvokeOnMainThread<T>(this IHasUIContext hasUIContext, Func<T> action) =>
        hasUIContext.UIContextHandler.InvokeOnMainThread(action);

    public static void BeginInvokeOnMainThread(this IHasUIContext hasUIContext, Action action) =>
        hasUIContext.UIContextHandler.BeginInvokeOnMainThread(action);

    public static void InvokeOnIdleMainThread(this IHasUIContext hasUIContext, Action action) =>
        hasUIContext.UIContextHandler.InvokeOnIdleMainThread(action);

    public static T InvokeOnIdleMainThread<T>(this IHasUIContext hasUIContext, Func<T> action) =>
        hasUIContext.UIContextHandler.InvokeOnIdleMainThread(action);

    public static async Task InvokeOnMainThreadAsync(this IHasUIContext hasUIContext, Action action) =>
        await hasUIContext.UIContextHandler.InvokeOnMainThreadAsync(action);

    public static async Task<T> InvokeOnMainThreadAsync<T>(this IHasUIContext hasUIContext, Func<T> action) =>
        await hasUIContext.UIContextHandler.InvokeOnMainThreadAsync(action);

    public static async Task<T> InvokeOnMainThreadAsync<T>(this IHasUIContext hasUIContext, Func<Task<T>> funcTask) =>
        await hasUIContext.UIContextHandler.InvokeOnMainThreadAsync(funcTask);

    public static async Task InvokeOnMainThreadAsync(this IHasUIContext hasUIContext, Func<Task> funcTask) =>
        await hasUIContext.UIContextHandler.InvokeOnMainThreadAsync(funcTask);

    public static void SendInThisOrMainThreadContext(this IHasUIContext hasUIContext,
                                                     Action action,
                                                     SynchronizationContext synchronizationContext = null,
                                                     uint timeout = 10000) =>
        hasUIContext.UIContextHandler.SendInThisOrMainThreadContext(action, synchronizationContext, timeout);

    public static async Task InvokeOnIdleMainThreadAsync(this IHasUIContext hasUIContext, Action action) =>
        await hasUIContext.UIContextHandler.InvokeOnIdleMainThreadAsync(action);

    public static async Task<T> InvokeOnIdleMainThreadAsync<T>(this IHasUIContext hasUIContext, Func<T> action) =>
        await hasUIContext.UIContextHandler.InvokeOnIdleMainThreadAsync(action);
}