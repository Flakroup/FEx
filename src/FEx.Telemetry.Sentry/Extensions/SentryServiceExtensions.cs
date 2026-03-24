using FEx.Telemetry.Sentry.Abstractions.Interfaces;
using System.Reflection;

namespace FEx.Telemetry.Sentry.Extensions;

public static class SentryServiceExtensions
{
    public static void InitializeFromConfig(this ISentryService service, FExSentryConfig config, Assembly appAssembly)
    {
        if (string.IsNullOrEmpty(config.SentryDsn))
            return;

        var version = appAssembly.GetName().Version?.ToString() ?? "0.0.0";
        service.Initialize(config.SentryDsn, config.SentryEnvironment, version);
    }
}
