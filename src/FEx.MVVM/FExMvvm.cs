using FEx.Abstractions.Interfaces;
using FEx.Common.Extensions;
using FEx.DI.Abstractions;
using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FEx.MVVM;

public class FExMvvm : InitializeModule<IFExMvvmContainer>
{
    private readonly IExceptionHandler _exceptionHandler;
    private static IMessagePopupService _messagePopupService;

    public static TimeSpan DefaultUIRefreshInterval { get; set; } = TimeSpan.FromMilliseconds(25);

    public static IMessagePopupService MessagePopupService
    {
        get => _messagePopupService.Guard();
        private set => _messagePopupService = value.Guard(nameof(value));
    }

    public FExMvvm(IMessagePopupService messagePopupService, IExceptionHandler exceptionHandler)
    {
        MessagePopupService = messagePopupService;
        _exceptionHandler = exceptionHandler;
    }

    protected override void AddServices(IFExMvvmContainer container, IServiceCollection services) =>
        FExMvvmModule.AddServices(container, services);

    protected override void OnInitialize()
    {
        base.OnInitialize();

        _exceptionHandler.Callback = (x, y) => MessagePopupService.ShowMessageAsync(x, informUser: y, wait: false);
    }
}