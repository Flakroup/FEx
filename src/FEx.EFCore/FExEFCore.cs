using FEx.DI.Abstractions;
using FEx.EFCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace FEx.EFCore;

public class FExEFCore : InitializeModule<IFExEFCoreModule>
{
    private readonly ISqlDbHelper _sqlDbHelper;

    public FExEFCore(ISqlDbHelper sqlDbHelper)
    {
        _sqlDbHelper = sqlDbHelper;
    }

    public override async Task OnCompleteInitializationAsync(IServiceCollection services) =>
        await _sqlDbHelper.InitializeAsync();

    protected override void AddServices(IFExEFCoreModule container, IServiceCollection services) =>
        FExEFCoreModule.AddServices(container, services);
}