using System;

namespace FEx.DependencyInjection.Abstractions.Interfaces;

public interface IFExStrongInjectServiceProvider : IFExServiceProvider
{
    TContainer ConfigureServiceProvider<TContainer>() where TContainer : class, IDisposable, new();
}