using FEx.DependencyInjection.Abstractions;

namespace FEx.Rx;

public class FExRxModuleInitializer : InitializeOnlyModule
{
    protected override void OnInitialize()
    {
        FExRx.Init();
    }
}