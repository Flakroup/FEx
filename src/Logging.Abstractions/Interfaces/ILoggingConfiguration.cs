using FEx.Logging.Abstractions.Enums;
using System;

namespace FEx.Logging.Abstractions.Interfaces;

public interface ILoggingConfiguration
{
    bool IsLoggingEnabled { get; }

    void Configure(Func<LoggingOptions, LoggingOptions> configurator);
    void EnableLogOption(LoggingOptions option);
    void DisableLogOption(LoggingOptions option);
    bool HasOption(LoggingOptions option);
}