using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FEx.Extensions;

public static class EventsExtensions
{
    public static void HandlePropertyChanged(this PropertyChangedEventHandler handler,
                                             object sender,
                                             string propertyName)
    {
        if (propertyName is null
            || handler is null)
            return;

        var e = new PropertyChangedEventArgs(propertyName);
        handler.HandlePropertyChanged(sender, e);
    }

    public static void HandlePropertyChanged(this PropertyChangedEventHandler handler,
                                             object sender,
                                             PropertyChangedEventArgs e)
    {
        if (e is null
            || handler is null)
            return;

        foreach (Delegate h in handler.GetInvocationList())
        {
            if (h.Target is ISynchronizeInvoke { InvokeRequired: true } synch)
                synch.Invoke(h, new[] { sender, e });
            else
                switch (h)
                {
                    case PropertyChangedEventHandler propertyChangedEventHandler:
                        propertyChangedEventHandler(sender, e);
                        break;
                    case EventHandler eventHandler:
                        eventHandler(sender, e);
                        break;
                    default:
                        throw new Exception($"{h.GetType().FullName} delegate type is not handled");
                }
        }
    }

    public static void HandleCollectionChanged(this NotifyCollectionChangedEventHandler handler,
                                               object sender,
                                               NotifyCollectionChangedEventArgs e)
    {
        if (e is null
            || handler is null)
            return;

        foreach (Delegate h in handler.GetInvocationList())
        {
            if (h.Target is ISynchronizeInvoke { InvokeRequired: true } synch)
                synch.Invoke(h, new[] { sender, e });
            else
                switch (h)
                {
                    case NotifyCollectionChangedEventHandler collectionChangedEventHandler:
                        collectionChangedEventHandler(sender, e);
                        break;
                    case EventHandler eventHandler:
                        eventHandler(sender, e);
                        break;
                    default:
                        throw new Exception($"{h.GetType().FullName} delegate type is not handled");
                }
        }
    }
}