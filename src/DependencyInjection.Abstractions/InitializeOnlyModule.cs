using Microsoft.Extensions.DependencyInjection;

namespace FEx.DependencyInjection.Abstractions;

public abstract class InitializeOnlyModule : InitializeModule<object, IServiceCollection>
{
    protected override void RegisterServices(object container, IServiceCollection services)
    {
        // Empty implementation for modules that don't need container access
    }

    protected override object GetModule() => null;
}