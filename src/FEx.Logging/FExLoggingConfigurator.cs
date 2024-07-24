using FEx.Abstractions;
using FEx.Logging.Extensions;
using Serilog;
using Serilog.Events;
using System;

namespace FEx.Logging;

public class FExLoggingConfigurator
{
    public string LogDirPath => FExFoundation.AppInfoProvider.LogDirPath;
    public string LogFilePath => FExFoundation.AppInfoProvider.LogFilePath;
    public bool ForceConsole { get; private set; }
    public LogEventLevel ExternalLoggingLevel { get; private set; }
    public LogEventLevel ExternalDebugLoggingLevel { get; private set; }
    public Func<LoggerConfiguration, LoggerConfiguration> CfgFunc { get; private set; }
    public string[] Overrides { get; private set; }

    public LoggerConfiguration Configuration { get; private set; }

    public FExLoggingConfigurator Set(bool forceConsole = false,
                                      LogEventLevel externalLoggingLevel = LogEventLevel.Warning,
                                      LogEventLevel externalDebugLoggingLevel = LogEventLevel.Information,
                                      Func<LoggerConfiguration, LoggerConfiguration> cfgFunc = null,
                                      params string[] overrides)
    {
        ForceConsole = forceConsole;
        ExternalLoggingLevel = externalLoggingLevel;
        ExternalDebugLoggingLevel = externalDebugLoggingLevel;
        CfgFunc = cfgFunc;
        Overrides = overrides;

        return this;
    }

    public FExLoggingConfigurator ConfigureSerilog()
    {
        Configuration = new LoggerConfiguration().ConfigureSerilog(LogFilePath,
            ForceConsole,
            ExternalLoggingLevel,
            ExternalDebugLoggingLevel,
            CfgFunc,
            Overrides);

        Log.Logger = Configuration.CreateLogger();

        return this;
    }
}