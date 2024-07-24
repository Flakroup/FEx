using System;

namespace FEx.Telemetry;

public interface IFExTelemetryConfig
{
    string AccessToken { get; set; }
    string AppEnvironment { get; }
    Func<string> PersonEmail { get; set; }
    Func<string> PersonUserName { get; set; }
    bool AddPersonToEnvironment { get; set; }
}