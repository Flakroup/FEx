using FEx.Logging.Abstractions.Interfaces;
using Serilog.Events;
using System;

namespace FEx.Logging;

public class DefaultPlatformLogger : IPlatformLogger
{
    public virtual void Log(string message, LogEvent logEvent)
    {
        if (logEvent.Level is LogEventLevel.Error or LogEventLevel.Fatal)
        {
            Console.Error.WriteLine(message);

            return;
        }

        Console.WriteLine(message);
    }
}