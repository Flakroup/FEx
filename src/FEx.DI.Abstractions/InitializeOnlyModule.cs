using Microsoft.Extensions.DependencyInjection;

namespace FEx.DI.Abstractions;

public abstract class InitializeOnlyModule : InitializeModule<object>
{
    protected override void AddServices(object container, IServiceCollection services)
    {
    }

    protected override object GetModule() => null;
}