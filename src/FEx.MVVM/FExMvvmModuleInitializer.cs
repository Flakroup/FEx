using FEx.DependencyInjection.Abstractions;
using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.MVVM;

public class FExMvvmModuleInitializer : InitializeModule<IFExMvvmModule>
{
    private readonly IMessagePopupService _messagePopupService;

    public FExMvvmModuleInitializer(IMessagePopupService messagePopupService)
    {
        _messagePopupService = messagePopupService;
    }

    protected override void OnInitialize()
    {
        FExMvvm.Init(_messagePopupService);
    }

    protected override void AddServices(IFExMvvmModule container, IServiceCollection services)
    {
        FExMvvmModule.AddServices(container, services);
    }
}