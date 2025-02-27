using FEx.Abstractions.Interfaces;
using FEx.DI.Abstractions.Interfaces;
using StrongInject;
using System;
using System.Diagnostics.CodeAnalysis;

namespace FEx.DependencyInjection;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExDependencyInjectionContainer : IContainer<IScopeProvider>, IContainer<IInitializeModule[]>,
    IContainer<IServiceProvider>, IContainer<IAsyncHelper>, IContainer<IFExServiceProvider>,
    IContainer<IFExInitialize[]>, IContainer<FExMicrosoftDIServiceProvider>, IContainer<IFExPriorityInitialize[]>
{
}