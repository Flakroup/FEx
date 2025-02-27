using FEx.DI.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.DependencyInjection;

public class FExDependencyInjection : InitializeModule<IFExDependencyInjectionContainer>
{
    protected override void AddServices(IFExDependencyInjectionContainer container, IServiceCollection services) =>
        FExDependencyInjectionModule.AddServices(container, services);
}