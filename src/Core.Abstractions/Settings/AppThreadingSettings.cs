using FEx.Core.Abstractions.Interfaces;

namespace FEx.Core.Abstractions.Settings;

public class AppThreadingSettings : IAppThreadingSettings
{
    public bool IsDeadlockMonitoringEnabled { get; set; }
}