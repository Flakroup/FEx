using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.EFCore.Tests;

/// <summary>
/// Satisfied by <c>SynchronizedDictionary.WaitForCacheTasksAsync</c>, so a fixture's private dictionary subclass only
/// has to name this interface.
/// </summary>
public interface IHasCacheTasks
{
    Task WaitForCacheTasksAsync();
}

/// <summary>
/// The dictionaries a fixture created. A dictionary saves in the background, and that save outlives the test body: a
/// fixture tears its database down only after <see cref="FlushAsync" />, because a save that finds the database gone
/// fails only in the log. The wait is not wrapped: a flush that throws fails the test.
/// </summary>
public sealed class CacheFlushes
{
    private readonly List<IHasCacheTasks> _tracked = [];

    public T Track<T>(T dictionary) where T : IHasCacheTasks
    {
        _tracked.Add(dictionary);

        return dictionary;
    }

    public async Task FlushAsync()
    {
        foreach (var dictionary in _tracked)
            await dictionary.WaitForCacheTasksAsync();
    }
}
