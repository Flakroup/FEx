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
                                                      Type callerType = null,
                                                      ISupportInitialize ownerWindow = null,
                                                      bool informUser = true,
                                                      string caption = "Something wrong happened",
                                                      bool wait = true,
                                                      MessageIcon messageBoxImage = MessageIcon.Exclamation,
                                                      FExMessageButton button = FExMessageButton.OK,
                                                      Stopwatch sw = null,
                                                      LogLevel level = LogLevel.Information,
                                                      Exception exception = null)
    {
        _logger.LogInformation($"{(callerType is null ? string.Empty : $"{callerType?.FullName} ")}{txt}");

        return await InternalShowMessageAsync(txt,
            callerType,
            ownerWindow,
            informUser,
            caption,
            wait,
            messageBoxImage,
            button,
            sw,
            level,
            exception);
    }

    public MessageResult ShowMessage(string txt,
                                     Type callerType = null,
                                     ISupportInitialize ownerWindow = null,
                                     bool informUser = true,
                                     string caption = "Something wrong happened",
                                     bool wait = true,
                                     MessageIcon messageBoxImage = MessageIcon.Exclamation,
                                     FExMessageButton button = FExMessageButton.OK,
                                     Stopwatch sw = null,
                                     LogLevel level = LogLevel.Information,
                                     Exception exception = null)
    {
        return FExAsyncx.AsyncHelper.FireOrWait(() => ShowMessageAsync(txt,
                callerType,
                ownerWindow,
                informUser,
                caption,
                wait,
                messageBoxImage,
                button,
                sw,
                level,
                exception),
            wait);
    }

    protected abstract Task<MessageResult> InternalShowMessageAsync(string txt,
                                                                    Type callerType = null,
                                                                    ISupportInitialize ownerWindow = null,
                                                                    bool informUser = true,
                                                                    string caption = "Something wrong happened",
                                                                    bool wait = true,
                                                                    MessageIcon messageBoxImage =
                                                                        MessageIcon.Exclamation,
                                                                    FExMessageButton button = FExMessageButton.OK,
                                                                    Stopwatch sw = null,
                                                                    LogLevel level = LogLevel.Information,
                                                                    Exception exception = null);
}