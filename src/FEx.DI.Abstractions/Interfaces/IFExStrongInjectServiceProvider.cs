using System;

namespace FEx.DI.Abstractions.Interfaces;

public interface IFExStrongInjectServiceProvider : IFExServiceProvider
{
    TContainer ConfigureServiceProvider<TContainer>() where TContainer : class, IDisposable, new();
}