using FEx.DependencyInjection.Abstractions;
using FEx.Flurlx.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Flurlx;

public class FExFlurlxModuleInitializer : InitializeModule<IFExFlurlxModule>
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

    protected override void AddServices(IFExFlurlxModule container, IServiceCollection services)
    {
        FExFlurlxModule.AddServices(container, services);
    }
}