using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Asyncx.Helpers;
using FEx.Core.Abstractions.CustomEventArgs;
using FEx.Core.Abstractions.Interfaces;
using FEx.Core.Collections.Concurrent;
using FEx.Telemetry.Rollbar.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using Rollbar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RollbarLogger = Rollbar.ILogger;

namespace FEx.Telemetry.Rollbar.Services;

public class RollbarService : IRollbarService
{
    private readonly IRollbarConfig _config;
    private readonly IAsyncHelper _asyncHelper;
    protected readonly IFExLogger _logger;

    public static IRollbar RollbarInstance => RollbarLocator.RollbarInstance;

    public RollbarLogger Logger => RollbarInstance.AsBlockingLogger(TimeSpan.FromSeconds(30));
    public bool IsConfigured => _config.IsConfigured;
    private SemaphoreSlim LoggingSemaphore { get; }
    private ConcurrentHashSet<string> LoggedExceptions { get; }

    public RollbarService(IFExLogger logger,
                          IRollbarConfig config,
                          IAsyncHelper asyncHelper,
                          IExceptionHandler exceptionHandler)
    {
        _logger = logger;
        _config = config;
        _asyncHelper = asyncHelper;

        LoggingSemaphore = new(1, 1);
        LoggedExceptions = [];

        exceptionHandler.ExceptionOccured += ExceptionOccured;
        RollbarInstance.InternalEvent += OnRollbarInternalEvent;
        //RollbarInfrastructure.Instance.QueueController!.InternalEvent += OnRollbarInternalEvent;
    }

    public void Error(Exception error, IDictionary<string, object> custom = null, bool force = false)
    {
        if (error is null
            || Debugger.IsAttached && !force)
            return;

        AddLogTask(() => IsExceptionUnique(error)
            ? Logger.Error(error, custom)
            : Logger);
    }

    public void SendMessage(string message, ErrorLevel level = ErrorLevel.Info, bool force = false)
    {
        if (Debugger.IsAttached
            && !force)
            return;

        AddLogTask(() => Logger.Log(level, new ObjectPackage(message)));
    }

    public void WaitForLoggingEnd()
    {
        if (!IsConfigured)
            return;

        JoinableAsyncHelper.AwaitWithoutDeadlock(WaitForLoggingEndAsync);
    }

    public async Task WaitForLoggingEndAsync()
    {
        if (!IsConfigured)
            return;

        await LoggingSemaphore.WaitAsync();

        try
        {
            //await LogTasks.WhenAllAsync();//todo resolve other way
        }
        finally
        {
            LoggingSemaphore.Release();
        }
    }

    public async Task OnExceptionOccuredAsync(Exception ex, params (string, object)[] custom) =>
        await OnExceptionOccuredAsync(ex,
            custom.IsNotNullOrEmptyList()
                ? custom.ToDictionary(x => x.Item1, x => x.Item2)
                : null);

    public async Task OnExceptionOccuredAsync(Exception ex, IDictionary<string, object> custom)
    {
        Error(ex, custom);
        await WaitForLoggingEndAsync();
    }

    private void AddLogTask(Func<RollbarLogger> func) =>
        _asyncHelper.FireTaskAndForget(() => HandleReportFuncAsync(func));

    [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods")]
    private async void ExceptionOccured(object sender, ExceptionEventArgs eventArgs) =>
        await OnExceptionOccuredAsync(eventArgs.Exception, eventArgs.Custom);

    private bool IsExceptionUnique(Exception error) => LoggedExceptions.Add(error.ToString());

    private async Task HandleReportFuncAsync(Func<RollbarLogger> func)
    {
        await LoggingSemaphore.WaitAsync();

        await AsyncStatics.DelayUntilAsync(() => !IsConfigured);

        try
        {
            func();
        }
        finally
        {
            LoggingSemaphore.Release();
        }
    }

    /// <summary>
    ///     Called when rollbar internal event is detected.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RollbarEventArgs" /> instance containing the event data.</param>
    private void OnRollbarInternalEvent(object sender, RollbarEventArgs e)
    {
        var message = e.TraceAsString();

        switch (e)
        {
            case RollbarApiErrorEventArgs or CommunicationErrorEventArgs or InternalErrorEventArgs:
                _logger.Error(message);

                break;
            default:
                _logger.Information(message);

                break;
        }
    }
}