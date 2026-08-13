using FEx.Logging.Abstractions.Interfaces;
using System;

namespace FEx.Telemetry.Sentry;

public class FExSentryConfig : FExTelemetryConfigBase, ISentryConfig
{
    public string SentryDsn { get; }
    public bool AttachScreenshot => false;
    public string EnvironmentId => AppEnvironment;
    public string SentryEnvironment => EnvironmentParam;

    public FExSentryConfig(string dsn, string envVarOverride = "SENTRY_DSN")
    {
        var envDsn = envVarOverride is not null
            ? Environment.GetEnvironmentVariable(envVarOverride)
            : null;

        SentryDsn = envDsn ?? dsn ?? string.Empty;
    }

    protected override void OnAccessTokenChanged(string accessToken) { }
}
