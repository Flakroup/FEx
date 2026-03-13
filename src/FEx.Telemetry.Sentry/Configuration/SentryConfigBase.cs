using FEx.Logging.Abstractions.Interfaces;

namespace FEx.Telemetry.Sentry.Configuration;

public abstract class SentryConfigBase : ISentryConfig
{
    public abstract string SentryDsn { get; }
    public virtual bool AttachScreenshot => false;
    public virtual string EnvironmentId => "production";
}
