using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Services;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FEx.Avaloniax;

public class AvaloniaMessagePopupService : MessagePopupServiceBase
{
    public AvaloniaMessagePopupService(ILogger<IMessagePopupService> logger)
        : base(logger)
    {
    }

    protected override async Task<MessageResult> InternalShowMessageAsync(string txt,
                                                                          string caption = "Something wrong happened",
                                                                          MessageIcon messageBoxImage =
                                                                              MessageIcon.Exclamation,
                                                                          FExMessageButton button = FExMessageButton.OK,
                                                                          ISupportInitialize ownerWindow = null,
                                                                          bool informUser = true,
                                                                          bool wait = true,
                                                                          Stopwatch sw = null,
                                                                          LogLevel level = LogLevel.Information,
                                                                          Exception exception = null) =>
        await Task.FromResult(MessageResult.OK);
}