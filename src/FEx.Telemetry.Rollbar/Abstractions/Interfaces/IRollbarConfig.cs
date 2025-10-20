using Rollbar;
using System;
using System.IO;

namespace FEx.Telemetry.Rollbar.Abstractions.Interfaces;

public interface IRollbarConfig : IFExTelemetryConfig
{
    DirectoryInfo LocalAppDataDir { get; }
    FileInfo DefaultRollbarStoreDbFile { get; }
    Version AppVersion { get; }
    RollbarInfrastructureConfig RollbarConfig { get; }
    bool IsConfigured { get; }
}