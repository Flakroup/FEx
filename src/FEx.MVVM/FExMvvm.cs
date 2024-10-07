using FEx.Common.Extensions;
using FEx.DependencyInjection.Abstractions;
using FEx.Fundamentals.Utilities;
using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FEx.MVVM;

public class FExMvvm : InitializeModule<IFExMvvmModule>
{
    public static TimeSpan DefaultUIRefreshInterval { get; set; } = TimeSpan.FromMilliseconds(25);
    public static IMessagePopupService MessagePopupService { get; private set; }

    public FExMvvm(IMessagePopupService messagePopupService)
    {
        MessagePopupService = messagePopupService.Guard(nameof(messagePopupService));
    }

    protected override void AddServices(IFExMvvmModule container, IServiceCollection services) =>
        FExMvvmModule.AddServices(container, services);

    protected override void OnInitialize()
    {
        ExceptionHandler.Callback = (x, y) => MessagePopupService.ShowMessageAsync(x, informUser: y, wait: false);
    }
}