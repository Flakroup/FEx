using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Core.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System;

namespace FEx.MVVM;

public class FExMvvm : FExInitializable
{
    private readonly IExceptionHandler _exceptionHandler;
    private static IMessagePopupService _messagePopupService;

    public static TimeSpan DefaultUIRefreshInterval { get; set; } = TimeSpan.FromMilliseconds(25);

    public static IMessagePopupService MessagePopupService
    {
        get => _messagePopupService.GuardProperty();
        private set => _messagePopupService = value.Guard(nameof(value));
    }

    public FExMvvm(IMessagePopupService messagePopupService, IExceptionHandler exceptionHandler)
    {
        MessagePopupService = messagePopupService;
        _exceptionHandler = exceptionHandler;
    }

    protected override void OnInitialize()
    {
        _exceptionHandler.Callback = (x, y) => MessagePopupService.ShowMessageAsync(x, "Something wrong happened", MessageIcon.Exclamation, FExMessageButton.OK, null, y, false, null, LogLevel.Information, null);
    }
}