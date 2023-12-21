using FEx.Extensions;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FEx.Basics.Extensions;

public static class EventsExtensions
{
    public static bool SetPropertyStatic<TRet, TObj>(this TObj sender,
                                                     ref TRet backingField,
                                                     TRet newValue,
                                                     Action<string> propertyChanged,
                                                     [CallerMemberName] string propertyName = null,
                                                     SynchronizationContext context = null)
        where TObj : INotifyPropertyChanged
    {
        Action<TObj, string, TRet> action = propertyChanged is not null && propertyName is not null
            ? (s, p, _) => s.OnPropertyChangedStatic(propertyChanged, p, context)
            : null;

        return sender.SetObjectProperty(ref backingField, newValue, action, propertyName);
    }

    public static void OnPropertyChangedStatic<TObj>(this TObj sender,
                                                     Action<string> propertyChanged,
                                                     [CallerMemberName] string propertyName = null,
                                                     SynchronizationContext context = null)
        where TObj : INotifyPropertyChanged
    {
        propertyChanged.Guard(nameof(propertyChanged));
        propertyName.Guard(nameof(propertyName));

        void EventDelegate() => propertyChanged(propertyName);
        FExBasics.EventDeliverer.DeliverEvent(EventDelegate, sender, context);
    }
}