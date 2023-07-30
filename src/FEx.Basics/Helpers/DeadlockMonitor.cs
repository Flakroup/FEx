using FEx.Basics.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;

namespace FEx.Basics.Helpers;

public static class DeadlockMonitor
{
    public static void Execute(Action action, uint timeout = 10000)
    {
        StackTrace stackTrace = FExBasics.StackTraceProvider.GetStackTrace();

        var timer = new Timer(Callback, stackTrace, timeout, Timeout.Infinite);
        try
        {
            action();
        }
        finally
        {
            timer?.Change(Timeout.Infinite, Timeout.Infinite);
            timer?.Dispose();
        }
    }

    private static void Callback(object state)
    {
        var stackTrace = (StackTrace)state;
        var ex = new AttachedException("Deadlock assumed, as no action could've been performed during timeout.",
            stackTrace);
        FExBasics.Logger.LogError(ex, ex.Message);
        throw ex;
    }
}
