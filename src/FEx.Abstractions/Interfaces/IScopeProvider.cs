using Microsoft.Extensions.DependencyInjection;

namespace FEx.Abstractions.Interfaces;

public interface IScopeProvider
{
    public IServiceScope CreateScope();
}