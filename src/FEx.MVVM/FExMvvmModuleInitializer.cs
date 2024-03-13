using FEx.Abstractions;
using FEx.MVVM.Abstractions.Interfaces;

namespace FEx.MVVM;

public class FExMvvmModuleInitializer : InitializeModule
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
}