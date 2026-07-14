using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FEx.MVVM.Abstractions;

public class DummyPopupService : IMessagePopupService
{
    public bool AppIsClosing { get; set; }

    public async Task<MessageResult> ShowMessageAsync(string txt,
                                                      string caption,
                                                      MessageIcon messageBoxImage,
                                                      FExMessageButton button,
                                                      ISupportInitialize? ownerWindow,
                                                      bool informUser,
                                                      bool wait,
                                                      Stopwatch? sw,
                                                      LogLevel level,
                                                      Exception? exception) =>
        await Task.FromResult(MessageResult.None);

    public MessageResult ShowMessage(string txt,
                                     string caption,
                                     MessageIcon messageBoxImage,
                                     FExMessageButton button,
                                     ISupportInitialize? ownerWindow,
                                     bool informUser,
                                     bool wait,
                                     Stopwatch? sw,
                                     LogLevel level,
                                     Exception? exception) =>
        MessageResult.None;
}