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

    public static void HandlePropertyChanged(this PropertyChangedEventHandler propertyChangedEventHandler,
                                             object sender,
                                             PropertyChangedEventArgs eventArgs)
    {
        propertyChangedEventHandler.HandleMulticastEvent(sender, eventArgs, handler => handler(sender, eventArgs));
    }

    public static void HandleCollectionChanged(this NotifyCollectionChangedEventHandler collectionChangedEventHandler,
                                               object sender,
                                               NotifyCollectionChangedEventArgs eventArgs)
    {
        collectionChangedEventHandler.HandleMulticastEvent(sender, eventArgs, handler => handler(sender, eventArgs));
    }

    public static void HandleMulticastEvent<THandler, TArgs>(this THandler handler,
                                                             object sender,
                                                             TArgs eventArgs,
                                                             Action<THandler> defaultInvocation)
        where THandler : MulticastDelegate where TArgs : EventArgs
    {
        if (eventArgs is null
            || handler is null)
            return;

        foreach (Delegate invocation in handler.GetInvocationList())
        {
            if (invocation.Target is ISynchronizeInvoke { InvokeRequired: true } synchronizeInvoke)
                synchronizeInvoke.Invoke(invocation, new[] { sender, eventArgs });
            else
                switch (invocation)
                {
                    case THandler collectionChangedEventHandler:
                        defaultInvocation(collectionChangedEventHandler);
                        break;
                    case EventHandler eventHandler:
                        eventHandler(sender, eventArgs);
                        break;
                    default:
                        throw new Exception($"{invocation.GetType().FullName} delegate type is not handled");
                }
        }
    }
}