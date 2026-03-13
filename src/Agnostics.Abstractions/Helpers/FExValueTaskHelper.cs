using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Helpers;

public static class FExValueTaskHelper
{
    public static ValueTask CompletedTask =>
#if NETSTANDARD
        new();
#else
        ValueTask.CompletedTask;
#endif
}