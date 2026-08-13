using FEx.Agnostics.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System;

namespace FEx.Logging.Abstractions.Interfaces;

public interface IFExLoggingService
{
    IFExLogger GetLogger<T>();
    IFExLogger GetLogger(object sender);
    void LogCritical<T>(string message, Exception? exception = null);
    void LogDebug<T>(string message, Exception? exception = null);
    void LogError<T>(string message, Exception? exception = null);
    void LogInformation<T>(string message, Exception? exception = null);
    void LogTrace<T>(string message, Exception? exception = null);
    void LogWarning<T>(string message, Exception? exception = null);
    void Log<T>(LogLevel logLevel, string message, Exception? exception = null);
    void LogCritical(object sender, string message, Exception? exception = null);
    void LogDebug(object sender, string message, Exception? exception = null);
    void LogError(object sender, string message, Exception? exception = null);
    void LogInformation(object sender, string message, Exception? exception = null);
    void LogTrace(object sender, string message, Exception? exception = null);
    void LogWarning(object sender, string message, Exception? exception = null);
    void Log(object sender, LogLevel logLevel, string message, Exception? exception = null);
}