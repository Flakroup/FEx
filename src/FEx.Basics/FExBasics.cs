using FEx.Abstractions;
using FEx.Basics.Abstractions.Interfaces;
using FEx.Basics.Helpers;
using FEx.Extensions;
using Microsoft.Extensions.Logging;

namespace FEx.Basics;

public class FExBasics
{
    public static IStackTraceProvider StackTraceProvider { get; private set; }
    public static IEventDeliverer EventDeliverer { get; private set; }
    public static ILogger Logger { get; private set; }
    public static AsyncHelper AsyncHelper { get; private set; }

    static FExBasics()
    {
        StackTraceProvider = new DefaultStackTraceProvider();
    }

    public static void Init(IStackTraceProvider stackTraceProvider,
                            IEventDeliverer eventDeliverer,
                            ILogger logger,
                            AsyncHelper asyncHelper)
    {
        StackTraceProvider = stackTraceProvider.Guard(nameof(stackTraceProvider));
        EventDeliverer = eventDeliverer.Guard(nameof(eventDeliverer));
        Logger = logger.Guard(nameof(logger));
        AsyncHelper = asyncHelper.Guard(nameof(asyncHelper));
    }
}