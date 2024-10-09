using Flurl.Http.Configuration;
using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Flurlx.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExFlurlxModule : IContainer<FExFlurlx>, IContainer<IFlurlConfigurator>,
    IContainer<IFlurlClientCache>, IContainer<IApiConfiguration>
{
}