using Microsoft.Extensions.DependencyInjection;

namespace FEx.Abstractions;

public interface IScopeProvider
{
    IServiceScope CreateScope();
}