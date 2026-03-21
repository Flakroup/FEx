using FEx.Agnostics.Abstractions.Extensions;
using FEx.Asyncx.Extensions;
using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FEx.MVVM.Services;

public abstract class MessagePopupServiceBase : IMessagePopupService
{
    protected readonly ILogger<IMessagePopupService> _logger;

    public bool AppIsClosing { get; set; }

    protected MessagePopupServiceBase(ILogger<MessagePopupServiceBase> logger)
    {
        _logger = logger.Guard(nameof(logger));
    }

    public async Task<MessageResult> ShowMessageAsync(string txt,
                                                      string caption,
                                                      MessageIcon messageBoxImage,
                                                      FExMessageButton button,
                                                      ISupportInitialize ownerWindow,
                                                      bool informUser,
                                                      bool wait,
                                                      Stopwatch sw,
                                                      LogLevel level,
                                                      Exception exception) =>
        await InternalShowMessageAsync(txt,
            caption,
            messageBoxImage,
            button,
            ownerWindow,
            informUser,
            wait,
            sw,
            level,
            exception);

    public MessageResult ShowMessage(string txt,
                                     string caption,
                                     MessageIcon messageBoxImage,
                                     FExMessageButton button,
                                     ISupportInitialize ownerWindow,
                                     bool informUser,
                                     bool wait,
                                     Stopwatch sw,
                                     LogLevel level,
                                     Exception exception) =>
        JoinableTaskExtensions.FireOrWait(() => ShowMessageAsync(txt,
                caption,
                messageBoxImage,
                button,
                ownerWindow,
                informUser,
                wait,
                sw,
                level,
                exception),
            wait);

    protected abstract Task<MessageResult> InternalShowMessageAsync(string txt,
                                                                    string caption,
                                                                    MessageIcon messageBoxImage,
                                                                    FExMessageButton button,
                                                                    ISupportInitialize ownerWindow,
                                                                    bool informUser,
                                                                    bool wait,
                                                                    Stopwatch sw,
                                                                    LogLevel level,
                                                                    Exception exception);
}
