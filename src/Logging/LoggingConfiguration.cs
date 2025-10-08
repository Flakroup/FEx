using FEx.Logging.Abstractions.Enums;
using FEx.Logging.Abstractions.Extensions;
using FEx.Logging.Abstractions.Interfaces;
using System;
using System.Diagnostics;

namespace FEx.Logging;

public class LoggingConfiguration : ILoggingConfiguration
{
    public bool IsLoggingEnabled => Options != 0;
    protected LoggingOptions Options { get; private set; }

    public LoggingConfiguration()
    {
        Options = Debugger.IsAttached
            ? LoggingOptions.DefaultDebugLog
            : LoggingOptions.DefaultReleaseLog;
    }

    public void EnableLogOption(LoggingOptions option) =>
        Configure(options => options.HasFlagFast(option)
            ? options
            : options | option);

    public void DisableLogOption(LoggingOptions option) =>
        Configure(options => !options.HasFlagFast(option)
            ? options
            : options & ~option);

    public bool HasOption(LoggingOptions option) => Options.HasFlagFast(option);

    public void Configure(Func<LoggingOptions, LoggingOptions> configurator) => Options = configurator(Options);
}