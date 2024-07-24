using FEx.Abstractions.Interfaces;
using FEx.DI.Abstractions.Interfaces;
using StrongInject;
using System;

namespace FEx.DependencyInjection;

public interface IFExDependencyInjectionModule : IContainer<FExStrongInjectServiceProvider>,
    IContainer<FExMicrosoftDIServiceProvider>, IContainer<IScopeProvider>, IContainer<IInitializeModule[]>,
    IContainer<IServiceProvider>, IContainer<IAsyncHelper>
{
}