using FEx.Abstractions.Interfaces;
using FEx.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace FEx.Logging.Abstractions;

public class FExLoggingFoundation : IFExInitialize
{
    private static ILoggerFactory _loggerFactory;
    private static ILogger _logger;

    public static ILoggerFactory LoggerFactory
    {
        get => _loggerFactory.Guard(nameof(_loggerFactory));
        private set => _loggerFactory = value.Guard(nameof(value));
    }

    public static ILogger Logger
    {
        get => _logger.Guard(nameof(_logger));
        private set => _logger = value.Guard(nameof(value));
    }

    public FExLoggingFoundation(ILoggerFactory loggerFactory, ILogger logger)
    {
        LoggerFactory = loggerFactory;
        Logger = logger;
    }

    public void Initialize()
    {
    }

    public static void Init(ILoggerFactory loggerFactory, ILogger logger)
    {
        LoggerFactory = loggerFactory;
        Logger = logger;
    }
}