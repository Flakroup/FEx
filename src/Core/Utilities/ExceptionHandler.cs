using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.CustomEventArgs;
using FEx.Core.Abstractions.Helpers;
using FEx.Core.Abstractions.Implementations;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FEx.Core.Utilities;

/// <summary>
/// Exception extensions class.
/// </summary>
public class ExceptionHandler : ExceptionHandlerBase
{
    private readonly IFExLogger _logger;

    /// <inheritdoc />
    public override event EventHandler<ExceptionEventArgs>? ExceptionOccured;

    public ExceptionHandler(IFExLogger logger)
    {
        _logger = logger;
    }

    public override void Handle(Exception exception, IExceptionHandlerOptions? options = null)
    {
        options ??= new ExceptionHandlerOptions();

        if (options.InformUser
            && exception is not TaskCanceledException)
            base.Handle(exception, options);
    }

    protected override void HandleException(Exception exception, IExceptionHandlerOptions? options)
    {
        // Base.Handle only reaches HandleException after Handle() coalesced options to a non-null instance.
        options = options.Guard(nameof(options));
        var args = new ExceptionEventArgs(exception, options.Custom);
        LastException = exception;
        ExceptionOccured?.Invoke(null, args);
        var infoSb = new StringBuilder();

        var info = infoSb.AppendLine()
            .AppendLine("Something really wrong happened:")
            .AppendLine(exception.BuildMessage())
            .ToString();

        LogError(exception, info);

        RunCallback(options, info);
    }

    private static void LogToFile(Exception exception)
    {
        try
        {
            var tempLog = new FileInfo(Path.Combine(Path.GetTempPath(),
                $"{FExCoreStatics.AppInfoProvider?.Name ?? "Flakroup"}.log"));

            using var str = tempLog.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var sw = new StreamWriter(str);
            sw.BaseStream.Seek(0, SeekOrigin.End);
            sw.WriteLine($"[{DateTime.Now}] {exception}");
        }
        catch
        {
            //ignored
        }
    }

    private void RunCallback(IExceptionHandlerOptions options, string info)
    {
        if (Callback is null)
            return;

        FExCoreStatics.AsyncHelper.FireTaskAndForget(() => Callback(info, options.InformUser || Debugger.IsAttached),
            AsyncMode.ThreadPool);
    }

    private void LogError(Exception exception, string info)
    {
        if (_logger is not null)
        {
            _logger.Error(exception, info);
        }
        else
        {
            if (Debugger.IsAttached)
                Debug.WriteLine(exception);

            if (ConsolePresent)
                Console.WriteLine(exception);

            LogToFile(exception);
        }
    }
}