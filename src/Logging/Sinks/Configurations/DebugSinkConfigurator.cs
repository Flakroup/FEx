using FEx.Logging.Abstractions;
using FEx.Logging.Abstractions.Enums;
using FEx.Logging.Abstractions.Interfaces;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace FEx.Logging.Sinks.Configurations;

public class DebugSinkConfigurator : SinkConfiguratorBase
{
    public string OutputTemplate { get; set; } =
        "[{Timestamp:HH:mm:ss.fff zzz} [{Level:u3}] <s:{SourceContext}> {Message:lj}{NewLine}{Exception}";

    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Verbose;

    public DebugSinkConfigurator(ILoggingConfiguration loggingConfiguration)
        : base(loggingConfiguration, LoggingOptions.Debug)
    {
    }

    public override LoggerConfiguration ConfigureSink(LoggerSinkConfiguration writeTo) =>
        writeTo.Debug(outputTemplate: OutputTemplate, restrictedToMinimumLevel: MinimumLevel);
}