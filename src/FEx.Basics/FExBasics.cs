using FEx.Extensions;

namespace FEx.Basics;

public static class FExBasics
{
    public static IStackTraceProvider StackTraceProvider { get; set; }

    static FExBasics()
    {
        StackTraceProvider = new DefaultStackTraceProvider();
    }

    public static void Init(IStackTraceProvider stackTraceProvider)
    {
        StackTraceProvider = stackTraceProvider.Guard();
    }
}