using FEx.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Fundamentals;

public class FExFundamentalsModuleInitializer : InitializeModule<IFExFundamentalsModule>
{
    protected override void OnInitialize()
    {
    }

    protected override void AddServices(IFExFundamentalsModule container, IServiceCollection services) => FExFundamentalsModule.AddServices(container, services);
}