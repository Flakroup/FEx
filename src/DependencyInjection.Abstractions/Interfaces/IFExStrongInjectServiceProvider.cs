using System;

namespace FEx.DependencyInjection.Abstractions.Interfaces;

public interface IFExStrongInjectServiceProvider : IFExServiceProvider
{
    void SetServiceProvider<TContainer>(TContainer container) where TContainer : class, IDisposable;
}