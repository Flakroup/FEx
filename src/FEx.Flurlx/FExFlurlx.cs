using FEx.DI.Abstractions;
using FEx.Flurlx.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Flurlx;

public class FExFlurlx : InitializeModule<IFExFlurlxContainer>
{
    public FExFlurlx(IFlurlConfigurator configurator)
        : base(configurator)
    {
    }

    protected override void AddServices(IFExFlurlxContainer container, IServiceCollection services) =>
        FExFlurlxModule.AddServices(container, services);
}