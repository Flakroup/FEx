using FEx.DependencyInjection.Abstractions.Basics;
using Microsoft.Extensions.Logging;
using System;

namespace FEx.Logging.Abstractions;

public class FExLoggingStatics : StaticsBase
{
    private static readonly ILoggerFactory? DefaultLoggerFactoryInstance;

    private static Func<ILoggerFactory>? _loggerFactoryFactory;

    /// <summary>
    /// Retrieves the <see cref="ILoggerFactory" /> instance.
    /// <br />
    /// <b>⚠️ This is discouraged</b> and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static ILoggerFactory LoggerFactory =>
        // DefaultLoggerFactoryInstance is null unless Initialize/Configure was called; Get<T>'s Guard() enforces
        // non-null at runtime and throws otherwise, so the null-forgiving operator here is safe.
        Get(_loggerFactoryFactory, static () => DefaultLoggerFactoryInstance!);

    static FExLoggingStatics()
    {
        DefaultLoggerFactoryInstance = null;
    }

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