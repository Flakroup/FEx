using FEx.Abstractions;
using FEx.Basics.Helpers;
using FEx.Fundamentals.Extensions;
using System;
using System.Threading;

namespace FEx.MVVM;

public class AsyncEventDeliverer : IEventDeliverer
{
    private static void InternalDeliverEvent(Action eventDelegate, object sender, SynchronizationContext context)
    {
        if (context is not null)
            context.SendInContext(sender, eventDelegate);
        else
            eventDelegate();
    }

    public void DeliverEvent(Action eventDelegate, object sender, SynchronizationContext context = null)
    {
        void EventDelegate() =>
            InternalDeliverEvent(eventDelegate, sender, context);

        DeadlockMonitor.Execute(EventDelegate);
    }
}