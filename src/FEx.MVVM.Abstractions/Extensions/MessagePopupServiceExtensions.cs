using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FEx.MVVM.Abstractions.Extensions;

public static class MessagePopupServiceExtensions
{
    public static Task<MessageResult> ShowMessageAsync(this IMessagePopupService service, string txt) =>
        service.ShowMessageAsync(txt, "Something wrong happened", MessageIcon.Exclamation, FExMessageButton.OK, null, true, true, null, LogLevel.Information, null);

    public static Task<MessageResult> ShowMessageAsync(this IMessagePopupService service, string txt, string caption) =>
        service.ShowMessageAsync(txt, caption, MessageIcon.Exclamation, FExMessageButton.OK, null, true, true, null, LogLevel.Information, null);

    public static Task<MessageResult> ShowMessageAsync(this IMessagePopupService service, string txt, string caption, MessageIcon messageBoxImage) =>
        service.ShowMessageAsync(txt, caption, messageBoxImage, FExMessageButton.OK, null, true, true, null, LogLevel.Information, null);

    public static Task<MessageResult> ShowMessageAsync(this IMessagePopupService service, string txt, string caption, MessageIcon messageBoxImage, FExMessageButton button) =>
        service.ShowMessageAsync(txt, caption, messageBoxImage, button, null, true, true, null, LogLevel.Information, null);

    public static MessageResult ShowMessage(this IMessagePopupService service, string txt) =>
        service.ShowMessage(txt, "Something wrong happened", MessageIcon.Exclamation, FExMessageButton.OK, null, true, true, null, LogLevel.Information, null);

    public static MessageResult ShowMessage(this IMessagePopupService service, string txt, string caption) =>
        service.ShowMessage(txt, caption, MessageIcon.Exclamation, FExMessageButton.OK, null, true, true, null, LogLevel.Information, null);

    public static MessageResult ShowMessage(this IMessagePopupService service, string txt, string caption, MessageIcon messageBoxImage) =>
        service.ShowMessage(txt, caption, messageBoxImage, FExMessageButton.OK, null, true, true, null, LogLevel.Information, null);

    public static MessageResult ShowMessage(this IMessagePopupService service, string txt, string caption, MessageIcon messageBoxImage, FExMessageButton button) =>
        service.ShowMessage(txt, caption, messageBoxImage, button, null, true, true, null, LogLevel.Information, null);
}
