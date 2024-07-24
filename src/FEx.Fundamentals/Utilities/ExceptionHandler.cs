using FEx.Abstractions;
using FEx.Abstractions.Enums;
using FEx.Abstractions.Interfaces;
using FEx.Basics.Implementations;
using FEx.Extensions;
using FEx.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FEx.Fundamentals.Utilities;

/// <summary>
///     Exception extensions class.
/// </summary>
public class ExceptionHandler : ExceptionHandlerBase
{
    public static EventHandler<ExceptionEventArgs> ExceptionOccured;

    private static bool? _consolePresent;

    /// <summary>
    ///     The last exception
    /// </summary>
    public static Exception LastException { get; set; }

    public static Func<string, bool, Task> Callback { get; set; }

    public static bool ConsolePresent
    {
        get
        {
            if (_consolePresent is null)
            {
                _consolePresent = true;

                try
                {
                    _ = Console.WindowHeight;
                }
                catch
                {
                    _consolePresent = false;
                }
            }

            return _consolePresent.Value;
        }
    }

    public override void Handle(Exception ex, IExceptionHandlerOptions options = null)
    {
        options ??= new ExceptionHandlerOptions();

        if (!options.InformUser
            && ex is not TaskCanceledException)
            base.Handle(ex, options);
    }

    protected override void HandleException(Exception exception, IExceptionHandlerOptions options)
    {
        var args = new ExceptionEventArgs(exception, options.Custom);
        ExceptionOccured?.Invoke(null, args);
        LastException = exception;
        var infoSb = new StringBuilder();

        var info = infoSb.AppendLine()
            .AppendLine("Something really wrong happened:")
            .AppendLine(exception.BuildMessage())
            .ToString();

        if (FExLoggingFoundation.Logger is not null)
        {
            FExLoggingFoundation.Logger.LogError(exception, info);
        }
        else
        {
            if (Debugger.IsAttached)
                Debug.WriteLine(exception);

            if (ConsolePresent)
                Console.WriteLine(exception);

            LogToFile(exception);
        }

        if (Callback is null)
            return;

        FExFoundation.AsyncHelper.FireTaskAndForget(() => Callback(info, options.InformUser || Debugger.IsAttached),
            AsyncMode.ThreadPool);
    }

    private static void LogToFile(Exception exception)
    {
        try
        {
            var tempLog = new FileInfo(Path.Combine(Path.GetTempPath(),
                $"{Assembly.GetEntryAssembly()?.GetName().Name ?? "Flakroup"}.log"));

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
}