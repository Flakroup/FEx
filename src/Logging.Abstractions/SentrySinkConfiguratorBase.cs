using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Enums;
using FEx.Logging.Abstractions.Interfaces;
using Sentry;
using Sentry.Serilog;
using Serilog.Events;

namespace FEx.Logging.Abstractions;

public abstract class SentrySinkConfiguratorBase : SinkConfiguratorBase
{
    protected readonly ISentryConfig _sentryConfig;
    private readonly IAppVersionProvider _appVersionProvider;

    protected SentrySinkConfiguratorBase(ISentryConfig sentryConfig,
                                         ILoggingConfiguration loggingConfiguration,
                                         IAppVersionProvider appVersionProvider)
        : base(loggingConfiguration, LoggingOptions.Sentry)
    {
        _sentryConfig = sentryConfig;
        _appVersionProvider = appVersionProvider;
    }

    protected virtual void ConfigureSentrySerilogLogging(SentrySerilogOptions options)
    {
        options.MinimumBreadcrumbLevel = LogEventLevel.Debug;
        options.MinimumEventLevel = LogEventLevel.Error;
        ConfigureSentryLogging(options);
    }

    protected virtual void ConfigureSentryLogging(SentryOptions options)
    {
        options.Dsn = _sentryConfig.SentryDsn;
        options.Debug = false;
        options.AutoSessionTracking = true;
        options.TracesSampleRate = 1.0;
        options.ProfilesSampleRate = 1.0;
        options.AttachStacktrace = true;
        options.SendDefaultPii = true;
        options.Release = _appVersionProvider.GetAppVersion();

        if (!_sentryConfig.EnvironmentId.IsNullOrEmpty())
            options.Environment = _sentryConfig.EnvironmentId;
    }
}