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
                                                      string caption = "Something wrong happened",
                                                      MessageIcon messageBoxImage = MessageIcon.Exclamation,
                                                      FExMessageButton button = FExMessageButton.OK,
                                                      ISupportInitialize ownerWindow = null,
                                                      bool informUser = true,
                                                      bool wait = true,
                                                      Stopwatch sw = null,
                                                      LogLevel level = LogLevel.Information,
                                                      Exception exception = null) =>
        await Task.FromResult(MessageResult.None);

    public MessageResult ShowMessage(string txt,
                                     string caption = "Something wrong happened",
                                     MessageIcon messageBoxImage = MessageIcon.Exclamation,
                                     FExMessageButton button = FExMessageButton.OK,
                                     ISupportInitialize ownerWindow = null,
                                     bool informUser = true,
                                     bool wait = true,
                                     Stopwatch sw = null,
                                     LogLevel level = LogLevel.Information,
                                     Exception exception = null) =>
        MessageResult.None;
}