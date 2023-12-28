using FEx.Abstractions;
using FEx.Asyncx.Helpers;
using FEx.Fundamentals;

namespace FEx.Asyncx;

public sealed class FExAsyncModuleInitializer : InitializeModule
{
    private readonly FExFundamentalsModuleInitializer _initializer;

    public FExAsyncModuleInitializer(FExFundamentalsModuleInitializer initializer)
    {
        _initializer = initializer;
    }

    protected override void OnInitialize()
    {
        _initializer.Initialize();
        JoinableAsyncHelper.SetMainJoinableTaskFactory(Foundation.MainThread);
    }
}