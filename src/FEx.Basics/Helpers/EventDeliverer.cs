using FEx.Abstractions.Interfaces;
using FEx.Basics.Extensions;
using System;
using System.Threading;

namespace FEx.Basics.Helpers;

public class EventDeliverer : IEventDeliverer
{
    public void DeliverEvent(Action eventDelegate, object sender, SynchronizationContext context = null)
    {
        DeadlockMonitor.Execute(EventDelegate);

        return;

        void EventDelegate() =>
            InternalDeliverEvent(eventDelegate, sender, context);
    }

    private static void InternalDeliverEvent(Action eventDelegate, object sender, SynchronizationContext context)
    {
        if (context is not null)
            context.SendInContext(sender, eventDelegate);
        else
            eventDelegate();
    }
}