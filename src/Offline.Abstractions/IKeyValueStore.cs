using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.Offline.Abstractions;

/// <summary>
/// The tiny persistence surface the offline kit builds on. A browser client backs it with
/// localStorage (via JS interop); tests use an in-memory dictionary; a native client could use
/// files. Values are opaque strings (the kit stores JSON).
/// </summary>
public interface IKeyValueStore
{
    Task<string?> GetAsync(string key);

    Task SetAsync(string key, string value);

    Task RemoveAsync(string key);

    /// <summary>All stored keys starting with <paramref name="prefix" />, in no particular order.</summary>
    Task<IReadOnlyList<string>> GetKeysAsync(string prefix);
}