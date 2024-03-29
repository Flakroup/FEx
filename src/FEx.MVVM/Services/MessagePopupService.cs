using FEx.Asyncx;
using FEx.Extensions;
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

    protected MessagePopupServiceBase(ILogger<IMessagePopupService> logger)
    {
        _logger = logger.Guard(nameof(logger));
    }

    public async Task<MessageResult> ShowMessageAsync(string txt,
                                                      string caption = "Something wrong happened",
                                                      MessageIcon messageBoxImage = MessageIcon.Exclamation,
                                                      FExMessageButton button = FExMessageButton.OK,
                                                      ISupportInitialize ownerWindow = null,
                                                      bool informUser = true,
                                                      bool wait = true,
                                                      Stopwatch sw = null,
                                                      LogLevel level = LogLevel.Information,
                                                      Exception exception = null) =>
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
                                     string caption = "Something wrong happened",
                                     MessageIcon messageBoxImage = MessageIcon.Exclamation,
                                     FExMessageButton button = FExMessageButton.OK,
                                     ISupportInitialize ownerWindow = null,
                                     bool informUser = true,
                                     bool wait = true,
                                     Stopwatch sw = null,
                                     LogLevel level = LogLevel.Information,
                                     Exception exception = null)
    {
        return FExAsyncx.AsyncHelper.FireOrWait(() => ShowMessageAsync(txt,
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
    }

    protected abstract Task<MessageResult> InternalShowMessageAsync(string txt,
                                                                    string caption = "Something wrong happened",
                                                                    MessageIcon messageBoxImage =
                                                                        MessageIcon.Exclamation,
                                                                    FExMessageButton button = FExMessageButton.OK,
                                                                    ISupportInitialize ownerWindow = null,
                                                                    bool informUser = true,
                                                                    bool wait = true,
                                                                    Stopwatch sw = null,
                                                                    LogLevel level = LogLevel.Information,
                                                                    Exception exception = null);
}