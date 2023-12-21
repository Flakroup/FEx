using System;
using System.Threading;

namespace FEx.Abstractions;

public interface IEventDeliverer
{
    void DeliverEvent(Action eventDelegate, object sender, SynchronizationContext context = null);
}