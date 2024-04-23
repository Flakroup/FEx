using FEx.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.DependencyInjection
{
    public class FExDependencyInjectionModuleInitializer : InitializeModule<IFExDependencyInjectionModule>
    {
        protected override void OnInitialize()
        {
        }

        protected override void AddServices(IFExDependencyInjectionModule container, IServiceCollection services) => FExDependencyInjectionModule.AddServices(container, services);
    }
}
