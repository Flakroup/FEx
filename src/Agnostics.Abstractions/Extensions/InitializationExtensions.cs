using FEx.Agnostics.Abstractions.Interfaces;
using System.Collections.Generic;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for initializing groups of objects.</summary>
public static class InitializationExtensions
{
    /// <summary>Initializes every object in a collection that is not initialized yet.</summary>
    /// <typeparam name="T">The initializable type.</typeparam>
    /// <param name="initializers">The objects to initialize; nothing happens when null or empty.</param>
    public static void InitializeAll<T>(this ICollection<T> initializers) where T : IFExInitializable
    {
        if (!(initializers?.Count > 0))
            return;

        foreach (var initializer in initializers)
        {
            if (!initializer.IsInitialized)
                initializer.Initialize();
        }
    }
}