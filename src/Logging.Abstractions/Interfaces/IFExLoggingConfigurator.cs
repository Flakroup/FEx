using FEx.DependencyInjection.Abstractions.Interfaces;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;

namespace FEx.Logging.Abstractions.Interfaces;

public interface IFExLoggingConfigurator : IConfigurator
{
    bool IsLoggingEnabled { get; }
    IList<string> Overrides { get; set; }
    LogEventLevel ExternalLoggingLevel { get; set; }
    LogEventLevel ExternalDebugLoggingLevel { get; set; }
    Func<LoggerConfiguration, LoggerConfiguration> CfgFunc { get; set; }
    LoggerConfiguration Configuration { get; }

    IEnumerable<FileInfo> GetLogFiles();
}