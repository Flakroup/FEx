using System;
using System.Threading;

namespace FEx.Abstractions.Interfaces;

public interface IEventDeliverer
{
    void DeliverEvent(Action eventDelegate, object sender, SynchronizationContext context = null);
}