using FEx.DependencyInjection.Abstractions;
using FEx.EFCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace FEx.EFCore;

public class FExEFCoreModuleInitializer : InitializeModule<IFExEFCoreModule>
{
    private readonly ISqlDbHelper _sqlDbHelper;

    public FExEFCoreModuleInitializer(ISqlDbHelper sqlDbHelper)
    {
        _sqlDbHelper = sqlDbHelper;
    }

    protected override void OnInitialize()
    {
    }

    protected override void AddServices(IFExEFCoreModule container, IServiceCollection services)
    {
        FExEFCoreModule.AddServices(container, services);
    }

    public override async Task OnCompleteInitializationAsync()
    {
        await _sqlDbHelper.InitializationTask;
    }
}