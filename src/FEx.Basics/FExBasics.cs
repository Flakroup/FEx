using FEx.Abstractions;
using FEx.Basics.Abstractions.Interfaces;
using FEx.Extensions;
using Microsoft.Extensions.Logging;

namespace FEx.Basics;

public class FExBasics
{
    public static IStackTraceProvider StackTraceProvider { get; private set; }
    public static IEventDeliverer EventDeliverer { get; private set; }
    public static ILogger Logger { get; private set; }

    public static bool SendEventsInCreationContext { get; set; }

    static FExBasics()
    {
        StackTraceProvider = new DefaultStackTraceProvider();
    }

    public static void Init(IStackTraceProvider stackTraceProvider,
                            IEventDeliverer eventDeliverer,
                            ILogger logger)
    {
        StackTraceProvider = stackTraceProvider.Guard(nameof(stackTraceProvider));
        EventDeliverer = eventDeliverer.Guard(nameof(eventDeliverer));
        Logger = logger.Guard(nameof(logger));
    }
}