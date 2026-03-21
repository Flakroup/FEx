using FEx.Core.Abstractions.Interfaces;
using System;

namespace FEx.Core.Abstractions.Extensions;

public static class FExDispatcherExtensions
{
    public static void SendInContext(this IFExDispatcher dispatcher, Action action, object sender) =>
        dispatcher.SendInContext(action, sender, 3000);
}
