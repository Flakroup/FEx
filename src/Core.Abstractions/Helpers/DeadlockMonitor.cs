using FEx.Agnostics.Abstractions.Utilities;
using FEx.Core.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;

namespace FEx.Core.Abstractions.Helpers;

public class DeadlockMonitor : IDeadlockMonitor
{
    private readonly IStackTraceProvider _stackTraceProvider;
    private readonly ILogger _logger;

    public DeadlockMonitor(IStackTraceProvider stackTraceProvider, ILogger logger)
    {
        _stackTraceProvider = stackTraceProvider;
        _logger = logger;
    }

    public void Execute(Action action, StackTrace stackTrace = null, uint timeout = 3000)
    {
        stackTrace ??= _stackTraceProvider.GetStackTrace();

        var timer = new Timer(state => Callback(state, timeout), stackTrace, timeout, Timeout.Infinite);

        try
        {
            action();
        }
        finally
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            timer.Dispose();
        }
    }

    private void Callback(object state, uint timeout = 3000)
    {
        var stackTrace = (StackTrace)state;

        var ex = new AttachedException(
            $"Deadlock assumed, as no action could've been performed during {TimeSpan.FromMilliseconds(timeout)} timeout.",
            stackTrace);

        _logger.LogError(ex);

        throw ex;
    }
}