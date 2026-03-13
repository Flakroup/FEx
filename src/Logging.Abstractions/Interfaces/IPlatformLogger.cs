using Serilog.Events;

namespace FEx.Logging.Abstractions.Interfaces;

public interface IPlatformLogger
{
    void Log(string message, LogEvent logEvent);
}