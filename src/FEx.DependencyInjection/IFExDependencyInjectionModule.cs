using FEx.DependencyInjection.Abstractions.Interfaces;
using StrongInject;
using System;

namespace FEx.DependencyInjection;

public interface IFExDependencyInjectionModule : IContainer<FExStrongInjectServiceProvider>,
    IContainer<FExMicrosoftDIServiceProvider>, IContainer<IScopeProvider>, IContainer<IInitializeModule[]>, IContainer<IServiceProvider>
{
}