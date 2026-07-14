using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FEx.Core.Abstractions.Extensions;

public static class EventsExtensions
{
    public static bool SetPropertyStatic<TRet, TObj>(this TObj sender,
                                                     ref TRet backingField,
                                                     TRet newValue,
                                                     Action<string> propertyChanged,
                                                     [CallerMemberName] string? propertyName = null)
        where TObj : INotifyPropertyChanged
    {
        Action<TObj, string?, TRet> action = propertyChanged is not null && propertyName is not null
            ? (s, p, _) => s.OnPropertyChangedStatic(propertyChanged, p)
            : static (_, _, _) => { };

        return sender.SetObjectProperty(ref backingField, newValue, action, propertyName);
    }

    public static void OnPropertyChangedStatic<TObj>(this TObj sender,
                                                     Action<string> propertyChanged,
                                                     [CallerMemberName] string? propertyName = null)
        where TObj : INotifyPropertyChanged
    {
        propertyChanged.Guard(nameof(propertyChanged));
        propertyName.Guard(nameof(propertyName));

#pragma warning disable CS0618 // Type or member is obsolete
        FExCoreStatics.Dispatcher.InvokeOnMainThread(() => propertyChanged(propertyName), sender);
#pragma warning restore CS0618 // Type or member is obsolete
    }
}