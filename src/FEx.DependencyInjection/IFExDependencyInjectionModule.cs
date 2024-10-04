using FEx.Abstractions.Interfaces;
using FEx.DI.Abstractions.Interfaces;
using StrongInject;
using System;
using System.Diagnostics.CodeAnalysis;

namespace FEx.DependencyInjection;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExDependencyInjectionModule : IContainer<FExStrongInjectServiceProvider>,
    IContainer<FExMicrosoftDIServiceProvider>, IContainer<IScopeProvider>, IContainer<IInitializeModule[]>,
    IContainer<IServiceProvider>, IContainer<IAsyncHelper>, IContainer<IFExServiceProvider>
{
}