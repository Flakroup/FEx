using FEx.Abstractions;
using FEx.Basics.Exceptions;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace FEx.Basics.Extensions;

public static class SynchronizationContextExtensions
{
    [SuppressMessage("Usage", "VSTHRD001:Avoid legacy thread switching APIs")]
    public static T SendInContext<T>(this SynchronizationContext context, object sender, Func<T> func)
    {
        StackTrace stackTrace = FExFoundation.StackTraceProvider.GetStackTrace();

        try
        {
            T res = default;

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
    public static void SendInContext(this SynchronizationContext context, object sender, Action action)
    {
        StackTrace stackTrace = FExFoundation.StackTraceProvider.GetStackTrace();

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