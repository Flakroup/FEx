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
                                         string caption = "Something wrong happened",
                                         MessageIcon messageBoxImage = MessageIcon.Exclamation,
                                         FExMessageButton button = FExMessageButton.OK,
                                         ISupportInitialize ownerWindow = null,
                                         bool informUser = true,
                                         bool wait = true,
                                         Stopwatch sw = null,
                                         LogLevel level = LogLevel.Information,
                                         Exception exception = null);

    MessageResult ShowMessage(string txt,
                              string caption = "Something wrong happened",
                              MessageIcon messageBoxImage = MessageIcon.Exclamation,
                              FExMessageButton button = FExMessageButton.OK,
                              ISupportInitialize ownerWindow = null,
                              bool informUser = true,
                              bool wait = true,
                              Stopwatch sw = null,
                              LogLevel level = LogLevel.Information,
                              Exception exception = null);
}