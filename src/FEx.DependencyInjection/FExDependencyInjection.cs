using FEx.DI.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.DependencyInjection;

public class FExDependencyInjection : InitializeModule<IFExDependencyInjectionModule>
{
    protected override void AddServices(IFExDependencyInjectionModule container, IServiceCollection services) =>
        FExDependencyInjectionModule.AddServices(container, services);
}