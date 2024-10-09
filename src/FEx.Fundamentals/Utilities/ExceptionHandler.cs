using FEx.Abstractions;
using FEx.Abstractions.CustomEventArgs;
using FEx.Abstractions.Enums;
using FEx.Abstractions.Interfaces;
using FEx.Basics.Implementations;
using FEx.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FEx.Fundamentals.Utilities;

/// <summary>
///     Exception extensions class.
/// </summary>
public class ExceptionHandler : ExceptionHandlerBase
{
    private readonly ILogger _logger;

    /// <inheritdoc />
    public override event EventHandler<ExceptionEventArgs> ExceptionOccured;

    public ExceptionHandler(ILogger logger)
    {
        _logger = logger;
    }

    public override void Handle(Exception exception, IExceptionHandlerOptions options = null)
    {
        options ??= new ExceptionHandlerOptions();

        if (!options.InformUser
            && exception is not TaskCanceledException)
            base.Handle(exception, options);
    }

    protected override void HandleException(Exception exception, IExceptionHandlerOptions options)
    {
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
                $"{(FExFoundation.HasBeenInitialized ? FExFoundation.AppInfoProvider.Name : null) ?? "Flakroup"}.log"));

            using FileStream str = tempLog.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
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

        FExFoundation.AsyncHelper.FireTaskAndForget(() => Callback(info, options.InformUser || Debugger.IsAttached),
            AsyncMode.ThreadPool);
    }

    private void LogError(Exception exception, string info)
    {
        if (_logger is not null)
        {
            _logger.LogError(exception, info);
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