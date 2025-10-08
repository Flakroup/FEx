using FEx.Logging.Abstractions.Enums;
using FEx.Logging.Abstractions.Interfaces;
using Serilog;
using Serilog.Configuration;

namespace FEx.Logging.Abstractions;

public abstract class SinkConfiguratorBase : ISinkConfigurator
{
    protected readonly ILoggingConfiguration _loggingConfiguration;

    public bool IsEnabled => _loggingConfiguration.HasOption(SinkType);
    public LoggingOptions SinkType { get; }

    protected SinkConfiguratorBase(ILoggingConfiguration loggingConfiguration, LoggingOptions sinkType)
    {
        _loggingConfiguration = loggingConfiguration;
        SinkType = sinkType;
    }

    public abstract LoggerConfiguration ConfigureSink(LoggerSinkConfiguration writeTo);
}