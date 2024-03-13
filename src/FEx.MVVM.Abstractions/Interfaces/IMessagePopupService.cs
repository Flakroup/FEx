using FEx.MVVM.Abstractions.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface IMessagePopupService
{
    bool AppIsClosing { get; set; }

    Task<MessageResult> ShowMessageAsync(string txt,
                                         Type callerType = null,
                                         ISupportInitialize ownerWindow = null,
                                         bool informUser = true,
                                         string caption = "Something wrong happened",
                                         bool wait = true,
                                         MessageIcon messageBoxImage = MessageIcon.Exclamation,
                                         FExMessageButton button = FExMessageButton.OK,
                                         Stopwatch sw = null,
                                         LogLevel level = LogLevel.Information,
                                         Exception exception = null);

    MessageResult ShowMessage(string txt,
                              Type callerType = null,
                              ISupportInitialize ownerWindow = null,
                              bool informUser = true,
                              string caption = "Something wrong happened",
                              bool wait = true,
                              MessageIcon messageBoxImage = MessageIcon.Exclamation,
                              FExMessageButton button = FExMessageButton.OK,
                              Stopwatch sw = null,
                              LogLevel level = LogLevel.Information,
                              Exception exception = null);
}