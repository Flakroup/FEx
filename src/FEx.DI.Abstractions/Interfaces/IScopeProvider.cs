using Microsoft.Extensions.DependencyInjection;

namespace FEx.DI.Abstractions.Interfaces;

public interface IScopeProvider
{
    public IServiceScope CreateScope();
}