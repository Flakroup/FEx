using Microsoft.Extensions.DependencyInjection;

namespace FEx.DependencyInjection.Abstractions;

public abstract class InitializeOnlyModule : InitializeModule<object>
{
    protected override void AddServices(object container, IServiceCollection services)
    {
    }
}