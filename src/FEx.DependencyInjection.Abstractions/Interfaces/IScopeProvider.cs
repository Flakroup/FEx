using Microsoft.Extensions.DependencyInjection;

namespace FEx.DependencyInjection.Abstractions.Interfaces;

public interface IScopeProvider
{
    IServiceScope CreateScope();
}