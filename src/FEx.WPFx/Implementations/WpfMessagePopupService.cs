using FEx.Basics.Collections.Concurrent;
using FEx.Fundamentals;
using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Services;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
#if NETFULL
using System.Security.Permissions;
#endif

namespace FEx.WPFx.Implementations;

public class WpfMessagePopupService : MessagePopupServiceBase, IDisposable
{
    protected ConcurrentHashSet<string> MessagesCache { get; }
    protected SemaphoreSlim MessagesCacheSemaphore { get; }

    public WpfMessagePopupService(ILogger<IMessagePopupService> logger)
        : base(logger)
    {
        MessagesCache = [];
        MessagesCacheSemaphore = new SemaphoreSlim(1, 1);
    }

    protected override async Task<MessageResult> InternalShowMessageAsync(string txt,
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
                                                                          Exception exception = null) =>
        await LogItAsync(txt,
            informUser,
            caption,
            wait,
            messageBoxImage,
            button,
            ownerWindow as Window,
            sw,
            level,
            exception);

    /// <summary>
    ///     Receives text to log and passes via event.
    /// </summary>
    /// <param name="txt">Text to be logged</param>
    /// <param name="informUser">Show message dialog if no textbox for log?</param>
    /// <param name="caption">The caption.</param>
    /// <param name="messageBoxImage">The message box image.</param>
    /// <param name="button">The buttons to show.</param>
    /// <param name="owner">The owner window.</param>
    /// <param name="sw">The stopwatch.</param>
    /// <param name="level">The level.</param>
    /// <param name="exception">The exception.</param>
    /// <returns></returns>
    protected async Task<MessageResult> LogItAsync(string txt,
                                                   bool informUser = true,
                                                   string caption = "Something wrong happened",
                                                   bool wait = true,
                                                   MessageIcon messageBoxImage = MessageIcon.Exclamation,
                                                   FExMessageButton button = FExMessageButton.OK,
                                                   Window owner = null,
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
                    owner,
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
                    owner,
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
                return (MessageResult)await Foundation.Dispatcher.InvokeOnMainThreadAsync(() =>
                    InternalShowMessageBox(message, caption, messageBoxImage, buttons, owner, sw));

            Foundation.Dispatcher.BeginInvokeOnMainThread(() =>
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

    private void Log(string txt, LogLevel level = LogLevel.Information, Exception exception = null)
    {
        _logger.Log(level, exception, txt);
    }

    #region IDisposable
    public void Dispose()
    {
        MessagesCacheSemaphore?.Dispose();
    }
    #endregion
}