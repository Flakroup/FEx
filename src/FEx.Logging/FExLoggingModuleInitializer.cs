using FEx.Abstractions;
using FEx.Fundamentals;

namespace FEx.Logging;

public class FExLoggingModuleInitializer : InitializeModule
{
    private readonly FExFundamentalsModuleInitializer _initializer;
    public FExLoggingModuleInitializer(FExFundamentalsModuleInitializer initializer)
    {
        _initializer = initializer;
    }

    protected override void OnInitialize()
    {
        _initializer.Initialize();
        LoggerExtensions.SetLogger();
    }
}