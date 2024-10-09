using FEx.Common.Abstractions.Interfaces;
using FEx.Common.Extensions;
using FEx.Extensions.IO;
using FEx.Logging.Extensions;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Diagnostics;
using System.IO;
using LoggerExtensions = FEx.Logging.Extensions.LoggerExtensions;

namespace FEx.Logging;

public class FExLoggingConfigurator
{
    private readonly IAppInfoProvider _appInfoProvider;

    private string _logDirPath;

    private string _logFilePath;

    public string LogDirPath
    {
        get => _logDirPath;
        set
        {
            if (_logDirPath is not null
                && _logDirPath.Equals(value.Guard(nameof(value))))
                return;

            _logDirPath = value;
            Directory.CreateDirectory(value!);
            SetLogFilePath(value);
        }
    }

    public string LogFilePath
    {
        get => _logFilePath;
        set
        {
            if (_logFilePath is not null
                && _logFilePath.Equals(value.Guard()))
                return;

            _logFilePath = value;
            SetLogDirPath(value);
        }
    }

    public bool ForceConsole { get; set; }
    public LogEventLevel ExternalLoggingLevel { get; set; }
    public LogEventLevel ExternalDebugLoggingLevel { get; set; }
    public Func<LoggerConfiguration, LoggerConfiguration> CfgFunc { get; set; }
    public string[] Overrides { get; set; }
    public LoggerConfiguration Configuration { get; set; }

    public FExLoggingConfigurator(IAppInfoProvider appInfoProvider)
    {
        _appInfoProvider = appInfoProvider;
        ExternalLoggingLevel = LogEventLevel.Warning;
        ExternalDebugLoggingLevel = LogEventLevel.Information;
        LogDirPath = (_appInfoProvider.UserData?.GetDescendantDirectory(".logs").FullName).Guard(nameof(LogDirPath));
    }

    public FExLoggingConfigurator ConfigureSerilog()
    {
        LoggerConfiguration cfg = Configuration ?? new LoggerConfiguration();

        if (Debugger.IsAttached)
        {
            cfg = cfg.MinimumLevel.Debug()
                .AddOverrides(Overrides, ExternalDebugLoggingLevel)
                .Enrich.FromLogContext()
                .WriteTo.Debug(outputTemplate: LoggerExtensions.DefaultConsoleOutputTemplate)
                .WriteTo.SetFileLogger(LogFilePath)
                .WriteTo.Console(outputTemplate: LoggerExtensions.DefaultConsoleOutputTemplate,
                    theme: AnsiConsoleTheme.Code);
        }
        else
        {
            cfg = cfg.MinimumLevel.Information()
                .AddOverrides(Overrides, ExternalLoggingLevel)
                .Enrich.FromLogContext()
                .WriteTo.Async(x => x.SetFileLogger(LogFilePath));

            if (ForceConsole)
                cfg = cfg.WriteTo.Console(outputTemplate: LoggerExtensions.DefaultConsoleOutputTemplate,
                    theme: AnsiConsoleTheme.Code);
        }

        Configuration = CfgFunc?.Invoke(cfg) ?? cfg;

        Log.Logger = Configuration.CreateLogger();
        Log.Information("#### Started Application ####");

        string rawCmd = Environment.CommandLine;
        string argsOnly = rawCmd.Replace($"\"{Environment.GetCommandLineArgs()[0]}\"", "").Trim();
        Log.Debug($"Startup args:{argsOnly}");

        return this;
    }

    private void SetLogFilePath(string logDirPath) =>
        LogFilePath = Path.Combine(logDirPath, $"{_appInfoProvider.Name}.log");

    private void SetLogDirPath(string logFilePath) => LogDirPath = Path.GetDirectoryName(logFilePath);
}