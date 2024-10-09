using FEx.Abstractions.Interfaces;
using System.Collections.Generic;

namespace FEx.Abstractions.Extensions;

public static class AbstractionsExtensions
{
    public static void InitializeAll<T>(this ICollection<T> initializers) where T : IFExInitialize
    {
        if (!(initializers?.Count > 0))
            return;

        foreach (T initializer in initializers)
        {
            if (!initializer.IsInitialized)
                initializer.Initialize();
        }
    }
}