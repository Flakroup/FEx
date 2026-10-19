using Microsoft.VisualStudio.Threading;
using System;
using System.Threading.Tasks;

namespace FEx.Building.Helpers;

/// <summary>
/// Minimal JoinableTask wrapper for running async code synchronously in NUKE targets without deadlocks.
/// </summary>
public static class JoinableTaskHelper
{
    private static readonly JoinableTaskContext Context = new();
    private static readonly JoinableTaskFactory Factory = Context.Factory;

    public static void Run(Func<Task> asyncMethod) => Factory.Run(asyncMethod);

    public static T Run<T>(Func<Task<T>> asyncMethod) => Factory.Run(asyncMethod);

    /// <summary>
    /// Starts the work without waiting for it, so a target can kick it off and a later one collect it with
    /// <see cref="JoinableTask{T}.Join(System.Threading.CancellationToken)" /> - on the same context, which
    /// is what keeps that later join deadlock-free.
    /// </summary>
    public static JoinableTask<T> RunAsync<T>(Func<Task<T>> asyncMethod) => Factory.RunAsync(asyncMethod);
}
