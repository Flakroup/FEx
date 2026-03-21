using FEx.MVVM.Abstractions.Interfaces;
using System;
using System.Threading.Tasks;

namespace FEx.MVVM.Abstractions.Extensions;

public static class FExTimerExtensions
{
    public static IFExTimer WithAsyncCallback(this IFExTimer timer, Func<Task> asyncCallback) =>
        timer.WithAsyncCallback(asyncCallback, default);
}
