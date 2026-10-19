using FEx.DependencyInjection.Abstractions.Basics;
using Microsoft.Extensions.Logging;
using System;

namespace FEx.Logging.Abstractions;

public class FExLoggingStatics : StaticsBase
{
    private static Func<ILoggerFactory>? _loggerFactoryFactory;

    /// <summary>
    /// Retrieves the <see cref="ILoggerFactory" /> instance.
    /// <br />
    /// <b>⚠️ This is discouraged</b> and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static ILoggerFactory LoggerFactory =>
        // There is no built-in default factory: the fallback returns null and Get<T>'s Guard() throws
        // ArgumentNullException unless Initialize/Configure supplied a factory (or a service provider is set).
        Get(_loggerFactoryFactory, static () => null!);

    public static void SetDefaults()
    {
        _loggerFactoryFactory = null;
    }

    public static void Configure(Func<ILoggerFactory>? loggerFactoryFactory = null)
    {
        if (loggerFactoryFactory is not null)
            _loggerFactoryFactory = loggerFactoryFactory;
    }

    public static void Initialize(ILoggerFactory loggerFactory, ILogger? logger = null) =>
        Configure(() => loggerFactory);
}