namespace FEx.Core.Abstractions.Interfaces;

public interface IAppThreadingSettings
{
    bool IsDeadlockMonitoringEnabled { get; }
}