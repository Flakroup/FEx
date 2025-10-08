using FEx.Logging.Abstractions;
using FEx.Logging.Abstractions.Enums;
using FEx.Logging.Abstractions.Interfaces;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace FEx.Logging.Sinks.Configurations;

public class ConsoleSinkConfigurator : SinkConfiguratorBase
{
    public string OutputTemplate { get; set; } =
        "[{Timestamp:HH:mm:ss.fff zzz} [{Level:u3}] <s:{SourceContext}> {Message:lj}{NewLine}{Exception}";

    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Verbose;
    public ConsoleTheme Theme { get; set; } = AnsiConsoleTheme.Code;

    public ConsoleSinkConfigurator(ILoggingConfiguration loggingConfiguration)
        : base(loggingConfiguration, LoggingOptions.Console)
    {
    }

    public override LoggerConfiguration ConfigureSink(LoggerSinkConfiguration writeTo) =>
        writeTo.Console(outputTemplate: OutputTemplate, theme: Theme, restrictedToMinimumLevel: MinimumLevel);
}