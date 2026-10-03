using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Helpers;

/// <summary>Helpers for <see cref="System.Threading.Tasks.ValueTask" /> across target frameworks.</summary>
public static class FExValueTaskHelper
{
    /// <summary>Gets an already completed <see cref="System.Threading.Tasks.ValueTask" />.</summary>
    public static ValueTask CompletedTask =>
#if NETSTANDARD
        new();
#else
        ValueTask.CompletedTask;
#endif
}