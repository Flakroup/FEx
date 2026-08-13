using FEx.Logging.Abstractions;
using FEx.Logging.Abstractions.Enums;
using FEx.Logging.Abstractions.Interfaces;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace FEx.Logging.Sinks.Configurations;

public class PlatformSinkConfigurator : SinkConfiguratorBase
{
    protected const string DefaultConsoleOutputTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

    private readonly IPlatformLogger _platformLogger;

    public PlatformSinkConfigurator(IPlatformLogger platformLogger, ILoggingConfiguration loggingConfiguration)
        : base(loggingConfiguration, LoggingOptions.Platform)
    {
        _platformLogger = platformLogger;
    }

    public override LoggerConfiguration ConfigureSink(LoggerSinkConfiguration writeTo) =>
        writeTo.Sink(new PlatformSink(new MessageTemplateTextFormatter(DefaultConsoleOutputTemplate), _platformLogger),
            LogEventLevel.Verbose);
}