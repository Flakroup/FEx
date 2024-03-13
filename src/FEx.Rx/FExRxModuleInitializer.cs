using FEx.Abstractions;

namespace FEx.Rx;

public class FExRxModuleInitializer : InitializeModule
{
    protected override void OnInitialize()
    {
        FExRx.Init();
    }
}