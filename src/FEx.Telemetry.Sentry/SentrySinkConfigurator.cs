using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Logging.Abstractions;
using FEx.Logging.Abstractions.Interfaces;
using Serilog;
using Serilog.Configuration;

namespace FEx.Telemetry.Sentry;

public class SentrySinkConfigurator : SentrySinkConfiguratorBase
{
    public SentrySinkConfigurator(ISentryConfig sentryConfig,
                                   ILoggingConfiguration loggingConfiguration,
                                   IAppVersionProvider appVersionProvider)
        : base(sentryConfig, loggingConfiguration, appVersionProvider)
    {
    }

    public override LoggerConfiguration ConfigureSink(LoggerSinkConfiguration writeTo) =>
        writeTo.Sentry(ConfigureSentrySerilogLogging);
}
