using FEx.Extensions;
using FEx.Fundamentals.Utilities;
using FEx.MVVM.Abstractions.Interfaces;
using System;

namespace FEx.MVVM;

public class FExMvvm
{
    public static TimeSpan DefaultUIRefreshInterval { get; set; } = TimeSpan.FromMilliseconds(25);
    public static IMessagePopupService MessagePopupService { get; private set; }

    public static void Init(IMessagePopupService messagePopupService)
    {
        MessagePopupService = messagePopupService.Guard(nameof(messagePopupService));
        ExceptionHandler.Callback = (x, y) => MessagePopupService.ShowMessageAsync(x, informUser: y, wait: false);
    }
}