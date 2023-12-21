using FEx.Abstractions;
using FEx.Extensions.Base.Implementations;

namespace FEx.Extensions.Base;

public static class FExExtensionsCommon
{
    public static IExceptionHandler ExceptionHandler { get; private set; }

    static FExExtensionsCommon()
    {
        ExceptionHandler = new DebugExceptionHandler();
    }

    public static void Initialize(IExceptionHandler exceptionHandler)
    {
        ExceptionHandler = exceptionHandler.Guard(nameof(exceptionHandler));
    }
}