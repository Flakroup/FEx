using FEx.Basics;
using FEx.Extensions;
using FEx.Logging.Extensions;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace FEx.Logging;

public class FExLoggingConfigurator
{
    public string LogDirPath { get; private set; }
    public string LogFilePath { get; private set; }
    public bool ForceConsole { get; private set; }
    public LogEventLevel ExternalLoggingLevel { get; private set; }
    public LogEventLevel ExternalDebugLoggingLevel { get; private set; }
    public Func<LoggerConfiguration, LoggerConfiguration> CfgFunc { get; private set; }
    public string[] Overrides { get; private set; }

    public LoggerConfiguration Configuration { get; private set; }

    public void Set(string logDirPath,
                    bool forceConsole = false,
                    LogEventLevel externalLoggingLevel = LogEventLevel.Warning,
                    LogEventLevel externalDebugLoggingLevel = LogEventLevel.Information,
                    Func<LoggerConfiguration, LoggerConfiguration> cfgFunc = null,
                    params string[] overrides)
    {
        logDirPath.Guard(nameof(logDirPath));
        LogDirPath = logDirPath;
        LogFilePath = Path.Combine(LogDirPath, $"{FExBasics.AppInfoProvider.Name}.log");
        ForceConsole = forceConsole;
        ExternalLoggingLevel = externalLoggingLevel;
        ExternalDebugLoggingLevel = externalDebugLoggingLevel;
        CfgFunc = cfgFunc;
        Overrides = overrides;
    }

    public void ConfigureSerilog()
    {
        Configuration = new LoggerConfiguration().ConfigureSerilog(LogFilePath,
            ForceConsole,
            ExternalLoggingLevel,
            ExternalDebugLoggingLevel,
            CfgFunc,
            Overrides);
        Log.Logger = Configuration.CreateLogger();
    }
}