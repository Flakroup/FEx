using Nuke.Common.Tools.DotNet;
using Serilog;
using System;

namespace FEx.Building;

public static class BuildExtensions
{
    public static void LogError<T>(this ILogger logger, T exception) where T : Exception =>
        logger.Error(exception, exception.ToString());

    public static void LogError(this Exception ex) => Log.Logger.Error(ex, ex.ToString());

    public static ILogger GetLogger(this object sender) => Log.Logger.ForContext(sender.GetType());

    /// <summary>
    /// Applies <c>--runtime</c> only when <paramref name="runtime" /> is non-null.
    /// Lets <c>Restore</c>/<c>Compile</c> targets stay runtime-agnostic for local
    /// dev runs while still producing runtime-specific <c>obj/</c> output when
    /// publish flow needs <c>--no-build --no-restore</c>.
    /// </summary>
    public static DotNetRestoreSettings WithRuntime(this DotNetRestoreSettings settings, string? runtime) =>
        runtime is not null
            ? settings.SetRuntime(runtime)
            : settings;

    /// <inheritdoc cref="WithRuntime(DotNetRestoreSettings, string?)" />
    public static DotNetBuildSettings WithRuntime(this DotNetBuildSettings settings, string? runtime) =>
        runtime is not null
            ? settings.SetRuntime(runtime)
            : settings;
}