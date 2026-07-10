using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Logging;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Core.Abstractions.Helpers;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Core.Abstractions.Extensions;

public static class SynchronizationContextExtensions
{
    public static SynchronizationContext? GetThreadSynchronizationContext(this Thread thread, bool createNew = false)
    {
        if (thread.ManagedThreadId.Equals(Environment.CurrentManagedThreadId))
            return Get(createNew);
#if NETFULL
        const string propertyName = "SynchronizationContext";
        return thread.ExecutionContext?.GetPropertyValue(propertyName) as SynchronizationContext;
#else
        const string fieldName = "_synchronizationContext";

        return thread.GetFieldValue(fieldName) as SynchronizationContext;
#endif
    }

    public static SynchronizationContext? Get(bool createNew = false)
    {
        if (SynchronizationContext.Current is null && createNew)
            SynchronizationContext.SetSynchronizationContext(new());

        return SynchronizationContext.Current;
    }

    [SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs")]
    public static T SendInContext<T>(this SynchronizationContext context, object? sender, Func<T> func)
    {
        var stackTrace = GetStackTrace();

        try
        {
            T res = default!;

            context.Send(_ =>
                {
                    try
                    {
                        res = func();
                    }
                    catch (Exception ex)
                    {
                        HandleAttachedException(sender, stackTrace, null, ex);

                        throw;
                    }
                },
                null);

            return res;
        }
        catch (Exception ex)
        {
            HandleAttachedException(sender, stackTrace, null, ex);

            throw;
        }
    }

    [SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs")]
    public static void SendInContext(this SynchronizationContext context, object? sender, Action action)
    {
        var stackTrace = GetStackTrace();

        try
        {
            context.Send(_ =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        HandleAttachedException(sender, stackTrace, null, ex);

                        throw;
                    }
                },
                null);
        }
        catch (Exception ex)
        {
            HandleAttachedException(sender, stackTrace, null, ex);

            throw;
        }
    }

    public static TaskCompletionSource<bool> PostInContext(this SynchronizationContext context,
                                                           Action action,
                                                           object? sender,
                                                           Action<AttachedException>? handleException = null)
    {
        _ = context.Guard(nameof(context));
        action.Guard(nameof(action));
        var stackTrace = FExCoreStatics.StackTraceProvider.GetStackTrace();
        var postFinished = new TaskCompletionSource<bool>();

        FExCoreStatics.AsyncHelper.FireTaskAndForget(() =>
            context.InternalPostInContextAsync(action, sender, postFinished, stackTrace, handleException));

        return postFinished;
    }

    private static StackTrace GetStackTrace() => FExCoreStatics.StackTraceProvider.GetStackTrace();

    private static void HandleAttachedException(object? sender,
                                                StackTrace callStack,
                                                Action<AttachedException>? onException,
                                                Exception ex)
    {
        // AttachedException.sender is non-null-annotated in L0, but tolerates a null diagnostic
        // sender at runtime; preserve the original (possibly-null) sender value.
        var aEx = new AttachedException(sender!, callStack, ex);

        if (onException is not null)
            onException(aEx);
        else if (FExCoreStatics.ExceptionHandler is not null)
            FExCoreStatics.ExceptionHandler.Handle(aEx);
        else
            throw aEx;
    }

    private static void Callback(object? state)
    {
        var stackTrace = (StackTrace)state!;

        var ex = new AttachedException("Deadlock assumed, as no action could've been performed during timeout.",
            stackTrace);

        FExStaticLogger.Error(ex, ex.Message);

        throw ex;
    }

    [SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs")]
    private static async Task<bool> InternalPostInContextAsync(this SynchronizationContext context,
                                                               Action action,
                                                               object? sender,
                                                               TaskCompletionSource<bool> postFinished,
                                                               StackTrace stackTrace,
                                                               Action<AttachedException>? onException = null)
    {
        try
        {
            var timer = new Timer(Callback, stackTrace, 10000, Timeout.Infinite);

            try
            {
                context.Post(_ => AwaitableInternalPost(action, sender, stackTrace, onException, postFinished), null);

#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
                return await postFinished.Task;
#pragma warning restore VSTHRD003
            }
            finally
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);

                // VSTHRD103: Timer.Dispose() is the intended synchronous cleanup here; the
                // analyzer's DisposeAsync suggestion fires on TFMs where the async overload exists
                // (net10/netstandard2.1). Suppressed unconditionally (a no-op where it does not fire).
#pragma warning disable VSTHRD103
                // ReSharper disable MethodHasAsyncOverload
                timer.Dispose();
                // ReSharper restore MethodHasAsyncOverload
#pragma warning restore VSTHRD103
            }
        }
        catch (Exception ex)
        {
            HandleAttachedException(sender, stackTrace, onException, ex);
            postFinished.TrySetResult(false);

            return false;
        }
    }

    private static void AwaitableInternalPost(Action action,
                                              object? sender,
                                              StackTrace stackTrace,
                                              Action<AttachedException>? onException,
                                              TaskCompletionSource<bool> postFinished)
    {
        var result = InternalPost(action, sender, stackTrace, onException);
        postFinished.SetResult(result.IsSuccess);
    }

    private static Result<ExceptionError> InternalPost(Action action,
                                                       object? sender,
                                                       StackTrace stackTrace,
                                                       Action<AttachedException>? onException)
    {
        try
        {
            action();

            return Result<ExceptionError>.Success;
        }
        catch (Exception ex)
        {
            HandleAttachedException(sender, stackTrace, onException, ex);

            return new ExceptionError(ex);
        }
    }
}