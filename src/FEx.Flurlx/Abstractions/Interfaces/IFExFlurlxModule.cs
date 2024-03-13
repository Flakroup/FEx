using Flurl.Http.Configuration;
using StrongInject;

namespace FEx.Flurlx.Abstractions.Interfaces;

public interface IFExFlurlxModule : IContainer<FExFlurlxModuleInitializer>, IContainer<IFlurlConfigurator>,
    IContainer<IFlurlClientCache>, IContainer<IApiConfiguration>
{
}