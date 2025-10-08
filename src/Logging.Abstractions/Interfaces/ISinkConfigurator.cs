using FEx.Logging.Abstractions.Enums;
using Serilog;
using Serilog.Configuration;

namespace FEx.Logging.Abstractions.Interfaces;

public interface ISinkConfigurator
{
    bool IsEnabled { get; }
    LoggingOptions SinkType { get; }

    LoggerConfiguration ConfigureSink(LoggerSinkConfiguration writeTo);
}