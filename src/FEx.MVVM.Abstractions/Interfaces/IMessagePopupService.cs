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
                                         string caption,
                                         MessageIcon messageBoxImage,
                                         FExMessageButton button,
                                         ISupportInitialize? ownerWindow,
                                         bool informUser,
                                         bool wait,
                                         Stopwatch? sw,
                                         LogLevel level,
                                         Exception? exception);

    MessageResult ShowMessage(string txt,
                              string caption,
                              MessageIcon messageBoxImage,
                              FExMessageButton button,
                              ISupportInitialize? ownerWindow,
                              bool informUser,
                              bool wait,
                              Stopwatch? sw,
                              LogLevel level,
                              Exception? exception);
}