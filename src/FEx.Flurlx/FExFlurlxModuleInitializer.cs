using FEx.Abstractions;
using FEx.Flurlx.Abstractions.Interfaces;

namespace FEx.Flurlx;

public class FExFlurlxModuleInitializer : InitializeModule
{
    private readonly IFlurlConfigurator _configurator;

    public FExFlurlxModuleInitializer(IFlurlConfigurator configurator)
    {
        _configurator = configurator;
    }

    protected override void OnInitialize()
    {
        _configurator.Configure();
    }
}