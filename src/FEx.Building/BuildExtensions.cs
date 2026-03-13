using System;
using Serilog;

namespace FEx.Building;

public static class BuildExtensions
{
    public static void LogError<T>(this ILogger logger, T exception)
        where T : Exception =>
        logger.Error(exception, exception.ToString());

    public static void LogError(this Exception ex) => Log.Logger.Error(ex, ex.ToString());

    public static ILogger GetLogger(this object sender) => Log.Logger.ForContext(sender.GetType());
}
