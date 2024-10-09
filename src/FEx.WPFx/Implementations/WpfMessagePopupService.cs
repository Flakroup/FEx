using FEx.Abstractions;
using FEx.Basics.Collections.Concurrent;
using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Services;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FEx.WPFx.Implementations;

public class WpfMessagePopupService : MessagePopupServiceBase, IDisposable
{
    protected ConcurrentHashSet<string> MessagesCache { get; }
    protected SemaphoreSlim MessagesCacheSemaphore { get; }

    public WpfMessagePopupService(ILogger<WpfMessagePopupService> logger)
        : base(logger)
    {
        MessagesCache = [];
        MessagesCacheSemaphore = new(1, 1);
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
                                                                          Exception exception = null)
    {
        try
        {
            Log(txt, level, exception);

            if (informUser && !AppIsClosing)
                return await ShowMessageBoxOnceAndCacheAsync(txt,
                    caption,
                    wait,
                    (MessageBoxImage)messageBoxImage,
                    (MessageBoxButton)button,
                    (Window)ownerWindow,
                    sw);
        }
        catch (Exception ex)
        {
            if (!AppIsClosing)
                return await ShowMessageBoxOnceAndCacheAsync(ex.ToString(),
                    caption,
                    wait,
                    (MessageBoxImage)messageBoxImage,
                    (MessageBoxButton)button,
                    (Window)ownerWindow,
                    sw);
        }

        return MessageResult.None;
    }

    private static MessageBoxResult InternalShowMessageBox(string message,
                                                           string caption,
                                                           MessageBoxImage messageBoxImage,
                                                           MessageBoxButton buttons,
                                                           Window owner,
                                                           Stopwatch sw)
    {
        sw?.Stop();

        try
        {
            owner ??= Application.Current.MainWindow;

            return owner is not null
                ? MessageBox.Show(owner, message, caption, buttons, messageBoxImage, MessageBoxResult.None)
                : MessageBox.Show(message, caption, buttons, messageBoxImage, MessageBoxResult.None);
        }
        finally
        {
            sw?.Start();
        }
    }

    private async Task<MessageResult> ShowMessageBoxOnceAndCacheAsync(string message,
                                                                      string caption = "Something wrong happened",
                                                                      bool wait = true,
                                                                      MessageBoxImage messageBoxImage =
                                                                          MessageBoxImage.Exclamation,
                                                                      MessageBoxButton buttons = MessageBoxButton.OK,
                                                                      Window owner = null,
                                                                      Stopwatch sw = null)
    {
        if (Application.Current is null
            || !MessagesCache.Add(message))
            return MessageResult.None;

        await MessagesCacheSemaphore.WaitAsync();

        try
        {
            if (wait)
                return (MessageResult)await FExFoundation.Dispatcher.InvokeOnMainThreadAsync(() =>
                    InternalShowMessageBox(message, caption, messageBoxImage, buttons, owner, sw));

            FExFoundation.Dispatcher.BeginInvokeOnMainThread(() =>
                InternalShowMessageBox(message, caption, messageBoxImage, buttons, owner, sw));
        }
        catch (Exception ex)
        {
            Log(ex.Message, LogLevel.Error, ex);

            //message cannot be displayed
        }
        finally
        {
            MessagesCache.Remove(message);
            MessagesCacheSemaphore.Release();
        }

        return MessageResult.None;
    }

    private void Log(string txt, LogLevel level = LogLevel.Information, Exception exception = null) =>
        _logger.Log(level, exception, txt);

    #region IDisposable
    public void Dispose() => MessagesCacheSemaphore?.Dispose();
    #endregion
}