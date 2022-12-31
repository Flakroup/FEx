using System;

namespace FEx.Telemetry;

public interface IFExTelemetryConfig
{
    public string RollbarAccessToken { get; set; }
    public string RollbarEnvironment { get; set; }
    public Func<string> RollbarPersonEmail { get; set; }
    public Func<string> RollbarPersonUserName { get; set; }
    public bool AddPersonToRollbarEnvironment { get; set; }
}