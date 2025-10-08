using FEx.DependencyInjection.Abstractions.Enums;
using FEx.Logging.Abstractions.Enums;
using FEx.Logging.Abstractions.Extensions;
using FEx.Logging.Abstractions.Interfaces;
using FEx.Logging.Sinks.Configurations;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FEx.Logging;

public class FExLoggingConfigurator : IFExLoggingConfigurator
{
    private readonly ILoggingConfiguration _loggingConfiguration;
    private readonly ISinkConfigurator[] _sinkConfigurators;

    public bool IsLoggingEnabled => _loggingConfiguration.IsLoggingEnabled;

    public ConfigurationPriority Priority { get; } = ConfigurationPriority.High;

    public IList<string> Overrides { get; set; } = ["Microsoft", "Microsoft.Hosting.Lifetime", "System"];
    public LogEventLevel ExternalLoggingLevel { get; set; } = LogEventLevel.Warning;
    public LogEventLevel ExternalDebugLoggingLevel { get; set; } = LogEventLevel.Information;
    public Func<LoggerConfiguration, LoggerConfiguration> CfgFunc { get; set; }
    public LoggerConfiguration Configuration { get; private set; }

    public FExLoggingConfigurator(ILoggingConfiguration loggingConfiguration, ISinkConfigurator[] sinkConfigurators)
    {
        _loggingConfiguration = loggingConfiguration;

        _sinkConfigurators = _loggingConfiguration.HasOption(LoggingOptions.Console)
                             && !sinkConfigurators.OfType<ConsoleSinkConfigurator>().Any()
            ? [.. sinkConfigurators, new ConsoleSinkConfigurator(_loggingConfiguration)]
            : sinkConfigurators;
    }

    public void Configure()
    {
        if (!IsLoggingEnabled)
            return;

        LogEventLevel baseLevel = Debugger.IsAttached
            ? LogEventLevel.Debug
            : LogEventLevel.Information;

        LogEventLevel externalLevel = Debugger.IsAttached
            ? ExternalDebugLoggingLevel
            : ExternalLoggingLevel;

        LoggerConfiguration cfg = _sinkConfigurators.Where(static sinkConfigurator => sinkConfigurator.IsEnabled)
            .Aggregate(new LoggerConfiguration()
                    .MinimumLevel.Is(baseLevel)
                    .AddOverrides(Overrides, externalLevel)
                    .Enrich.FromLogContext(),
                static (loggerConfiguration, sinkConfigurator) =>
                    sinkConfigurator.ConfigureSink(loggerConfiguration.WriteTo));

        Configuration = CfgFunc?.Invoke(cfg) ?? cfg;
        Log.Logger = Configuration.CreateLogger();

        Log.Information("#### Started Application ####");

        string rawCmd = Environment.CommandLine;
        string argsOnly = rawCmd.Replace($"\"{Environment.GetCommandLineArgs()[0]}\"", "").Trim();
        Log.Debug($"Startup args:{argsOnly}");
    }

    public IEnumerable<FileInfo> GetLogFiles() =>
        _sinkConfigurators.OfType<IFileSinkConfigurator>().SelectMany(static fileSink => fileSink.GetLogFiles());
}