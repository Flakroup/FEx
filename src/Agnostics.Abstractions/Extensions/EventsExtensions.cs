using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions that raise events and marshal each handler to its synchronization context when needed.</summary>
public static class EventsExtensions
{
    /// <summary>Raises a property changed event.</summary>
    /// <param name="handler">The handler to invoke; nothing happens when null.</param>
    /// <param name="sender">The event sender.</param>
    /// <param name="propertyName">The name of the changed property; nothing happens when null.</param>
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

    /// <summary>Raises a property changed event.</summary>
    /// <param name="propertyChangedEventHandler">The handler to invoke; nothing happens when null.</param>
    /// <param name="sender">The event sender.</param>
    /// <param name="eventArgs">The event data.</param>
    public static void HandlePropertyChanged(this PropertyChangedEventHandler propertyChangedEventHandler,
                                             object sender,
                                             PropertyChangedEventArgs eventArgs) =>
        propertyChangedEventHandler.HandleMulticastEvent(sender, eventArgs, handler => handler(sender, eventArgs));

    /// <summary>Raises a collection changed event.</summary>
    /// <param name="collectionChangedEventHandler">The handler to invoke; nothing happens when null.</param>
    /// <param name="sender">The event sender.</param>
    /// <param name="eventArgs">The event data.</param>
    public static void HandleCollectionChanged(this NotifyCollectionChangedEventHandler collectionChangedEventHandler,
                                               object sender,
                                               NotifyCollectionChangedEventArgs eventArgs) =>
        collectionChangedEventHandler.HandleMulticastEvent(sender, eventArgs, handler => handler(sender, eventArgs));

    /// <summary>Invokes each handler of a multicast event, marshaling the call through <see cref="System.ComponentModel.ISynchronizeInvoke" /> when its target requires it.</summary>
    /// <typeparam name="THandler">The delegate type.</typeparam>
    /// <typeparam name="TArgs">The event data type.</typeparam>
    /// <param name="handler">The multicast handler; nothing happens when null.</param>
    /// <param name="sender">The event sender.</param>
    /// <param name="eventArgs">The event data; nothing happens when null.</param>
    /// <param name="defaultInvocation">Invokes a handler directly when no marshaling is needed.</param>
    public static void HandleMulticastEvent<THandler, TArgs>(this THandler handler,
                                                             object sender,
                                                             TArgs eventArgs,
                                                             Action<THandler> defaultInvocation)
        where THandler : MulticastDelegate where TArgs : EventArgs
    {
        if (eventArgs is null
            || handler is null)
            return;

        foreach (var invocation in handler.GetInvocationList())
        {
            if (invocation.Target is ISynchronizeInvoke { InvokeRequired: true } synchronizeInvoke)
                synchronizeInvoke.Invoke(invocation, [sender, eventArgs]);
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
                        throw new($"{invocation.GetType().FullName} delegate type is not handled");
                }
        }
    }
}