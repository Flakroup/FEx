using Microsoft.Extensions.Logging;
using NuGet.Common;
using System.Threading.Tasks;
using INuGetLogger = NuGet.Common.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using NuGetLogLevel = NuGet.Common.LogLevel;

namespace FEx.NuGetx;

public class NuGetLogger<T> : INuGetLogger
{
    private readonly ILogger<T> _logger;

    public NuGetLogger(ILogger<T> logger)
    {
        _logger = logger;
    }

    public void LogDebug(string data) => Dump(LogLevel.Debug, $"DEBUG: {data}");

    public void LogVerbose(string data) => Dump(LogLevel.Trace, $"VERBOSE: {data}");

    public void LogInformation(string data) => Dump(LogLevel.Information, $"INFORMATION: {data}");

    public void LogMinimal(string data) => Dump(LogLevel.Information, $"MINIMAL: {data}");

    public void LogWarning(string data) => Dump(LogLevel.Warning, $"WARNING: {data}");

    public void LogError(string data) => Dump(LogLevel.Error, $"ERROR: {data}");

    public void LogInformationSummary(string data) => Dump(LogLevel.Information, $"INFORMATION SUMMARY: {data}");

    public void Log(NuGetLogLevel level, string data) =>
        Dump(GetLogLevel(level), $"{level.ToString().ToUpper()}: {data}");

    public void Log(ILogMessage message) => Dump(GetLogLevel(message.Level), $"{message.Level}: {message}");

    public Task LogAsync(NuGetLogLevel level, string data)
    {
        Log(level, data);

        return Task.CompletedTask;
    }

    public Task LogAsync(ILogMessage message)
    {
        Log(message);

        return Task.CompletedTask;
    }

    public void LogSummary(string data) => Dump(LogLevel.Information, $"SUMMARY: {data}");

    public void Dump(LogLevel logLevel, string content) => _logger.Log(logLevel, content);

    private static LogLevel GetLogLevel(NuGetLogLevel level)
    {
        return level switch
        {
            NuGetLogLevel.Debug or NuGetLogLevel.Verbose => LogLevel.Debug,
            NuGetLogLevel.Information or NuGetLogLevel.Minimal => LogLevel.Information,
            NuGetLogLevel.Warning => LogLevel.Warning,
            NuGetLogLevel.Error => LogLevel.Error,
            _ => LogLevel.None
        };
    }
}