using Microsoft.VisualStudio.Threading;
using System;
using System.Threading.Tasks;

namespace FEx.Building.Helpers;

/// <summary>
/// Minimal JoinableTask wrapper for running async code synchronously in NUKE targets without deadlocks.
/// </summary>
public static class JoinableTaskHelper
{
    private static readonly JoinableTaskContext _context = new();
    private static readonly JoinableTaskFactory _factory = _context.Factory;

    public static void Run(Func<Task> asyncMethod) => _factory.Run(asyncMethod);

    public static T Run<T>(Func<Task<T>> asyncMethod) => _factory.Run(asyncMethod);
}