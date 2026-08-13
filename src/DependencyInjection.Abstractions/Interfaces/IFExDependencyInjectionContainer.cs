using FEx.Agnostics.Abstractions.Interfaces;
using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.DependencyInjection.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExDependencyInjectionContainer : IContainer<IFExServiceContainer>, IContainer<IFExInitializable[]>,
    IContainer<IMicrosoftDIConfigurator[]>, IContainer<IConfigurator[]>, IContainer<IAsyncConfigurator[]>,
    IContainer<IFExStrongInjectServiceProvider>, IContainer<IFExServiceProvider[]>
{
}