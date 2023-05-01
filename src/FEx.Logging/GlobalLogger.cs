using System;

namespace FEx.Logging;

public class GlobalLogger
{
    private static readonly FExLogger Logger;

    static GlobalLogger()
    {
        Logger = new();
    }

    public static void LogInformation(string message)
    {
        Logger.LogInformation(message);
    }

    public static void LogError(string message, Exception ex = null)
    {
        Logger.LogError(message, ex);
    }
}