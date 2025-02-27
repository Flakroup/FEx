using Flurl.Http.Configuration;
using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Flurlx.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExFlurlxContainer : IContainer<FExFlurlx>, IContainer<IFlurlConfigurator>,
    IContainer<IFlurlClientCache>, IContainer<IApiConfiguration>
{
}