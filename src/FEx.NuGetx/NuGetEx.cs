using FEx.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.NuGetx;

public class NuGetEx : InitializeModule<INuGetExModule, IServiceCollection>
{
    protected override void RegisterServices(INuGetExModule container, IServiceCollection services) =>
        NuGetExModule.AddServices(container, services);
}