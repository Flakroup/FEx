using FEx.Flurlx.Abstractions.Interfaces;
using Flurl.Http.Configuration;
using StrongInject;

namespace FEx.Flurlx;

public interface IFExFlurlxModule : IContainer<FExFlurlxModuleInitializer>, IContainer<IFlurlConfigurator>,
    IContainer<IFlurlClientCache>, IContainer<IApiConfiguration>
{
}