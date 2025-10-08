using FEx.DependencyInjection.Abstractions.Basics;
using Microsoft.Extensions.Logging;
using System;

namespace FEx.Logging.Abstractions;

public class FExLoggingStatics : StaticsBase
{
    private static readonly ILoggerFactory _defaultLoggerFactoryInstance;

    private static Func<ILoggerFactory> _loggerFactoryFactory;

    /// <summary>
    /// Retrieves the <see cref="ILoggerFactory" /> instance.
    /// <br />
    /// <b>⚠️ This is discouraged</b> and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static ILoggerFactory LoggerFactory =>
        Get(_loggerFactoryFactory, static () => _defaultLoggerFactoryInstance);

    static FExLoggingStatics()
    {
        _defaultLoggerFactoryInstance = null;
    }

    public static void SetDefaults()
    {
        _loggerFactoryFactory = null;
    }

    public static void Configure(Func<ILoggerFactory> loggerFactoryFactory = null)
    {
        if (loggerFactoryFactory is not null)
            _loggerFactoryFactory = loggerFactoryFactory;
    }

    public static void Initialize(ILoggerFactory loggerFactory, ILogger logger = null) =>
        Configure(() => loggerFactory);
}