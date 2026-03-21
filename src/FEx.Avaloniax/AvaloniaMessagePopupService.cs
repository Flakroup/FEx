using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Services;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FEx.Avaloniax;

public class AvaloniaMessagePopupService : MessagePopupServiceBase
{
    public AvaloniaMessagePopupService(ILogger<AvaloniaMessagePopupService> logger)
        : base(logger)
    {
    }

    protected override async Task<MessageResult> InternalShowMessageAsync(string txt,
                                                                          string caption,
                                                                          MessageIcon messageBoxImage,
                                                                          FExMessageButton button,
                                                                          ISupportInitialize ownerWindow,
                                                                          bool informUser,
                                                                          bool wait,
                                                                          Stopwatch sw,
                                                                          LogLevel level,
                                                                          Exception exception) =>
        await Task.FromResult(MessageResult.OK);
}
