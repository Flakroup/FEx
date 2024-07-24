using FEx.Abstractions;
using FEx.Abstractions.Flow;
using FEx.Abstractions.Flow.Errors;
using FEx.Basics.Exceptions;
using FEx.Basics.Extensions;
using FEx.Common.Extensions;
using FEx.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Asyncx.Extensions;

public static class SynchronizationContextExtensions
{
    public static TaskCompletionSource<bool> PostInContext(this SynchronizationContext context,
                                                           Action action,
                                                           object sender,
                                                           Action<AttachedException> handleException = null)
    {
        _ = context.Guard(nameof(context));
        action.Guard(nameof(action));
        StackTrace stackTrace = FExFoundation.StackTraceProvider.GetStackTrace();
        var postFinished = new TaskCompletionSource<bool>();

        FExFoundation.AsyncHelper.FireTaskAndForget(() =>
            context.InternalPostInContextAsync(action, sender, postFinished, stackTrace, handleException));

        return postFinished;
    }

    private static void Callback(object state)
    {
        var stackTrace = (StackTrace)state;

        var ex = new AttachedException("Deadlock assumed, as no action could've been performed during timeout.",
            stackTrace);

        FExLoggingFoundation.Logger.LogError(ex, ex.Message);

        throw ex;
    }

    [SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs")]
    private static async Task<bool> InternalPostInContextAsync(this SynchronizationContext context,
                                                               Action action,
                                                               object sender,
                                                               TaskCompletionSource<bool> postFinished,
                                                               StackTrace stackTrace,
                                                               Action<AttachedException> onException = null)
    {
        try
        {
            var timer = new Timer(Callback, stackTrace, 10000, Timeout.Infinite);

            try
            {
                context.Post(_ => AwaitableInternalPost(action, sender, stackTrace, onException, postFinished), null);

                return await postFinished.Task;
            }
            finally
            {
                timer?.Change(Timeout.Infinite, Timeout.Infinite);

#if NET
#pragma warning disable VSTHRD103
#endif
                // ReSharper disable MethodHasAsyncOverload
                timer?.Dispose();
                // ReSharper restore MethodHasAsyncOverload
#if NET
#pragma warning restore VSTHRD103
#endif
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
                                              object sender,
                                              StackTrace stackTrace,
                                              Action<AttachedException> onException,
                                              TaskCompletionSource<bool> postFinished)
    {
        Result<ExceptionError> result = InternalPost(action, sender, stackTrace, onException);
        postFinished.SetResult(result.IsSuccess);
    }

    private static Result<ExceptionError> InternalPost(Action action,
                                                       object sender,
                                                       StackTrace stackTrace,
                                                       Action<AttachedException> onException)
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

    private static void HandleAttachedException(object sender,
                                                StackTrace callStack,
                                                Action<AttachedException> onException,
                                                Exception ex)
    {
        var aEx = new AttachedException(sender, callStack, ex);

        if (onException is not null)
            onException(aEx);
        else if (FExFoundation.ExceptionHandler is not null)
            aEx.HandleException();
        else
            throw aEx;
    }
}