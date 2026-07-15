using FEx.Offline.Abstractions;
using System.Text.Json;
using System.Threading.Tasks;

namespace FEx.Offline;

/// <summary>
/// Autosaves in-progress form input so a tab kill or crash never loses a half-typed draft. One draft
/// per key; saving overwrites, submitting clears.
/// </summary>
public sealed class DraftStore
{
    private const string KeyPrefix = "draft:";

    private readonly IKeyValueStore _store;
    private readonly JsonSerializerOptions _json;

    public DraftStore(IKeyValueStore store, JsonSerializerOptions? json = null)
    {
        _store = store;
        _json = json ?? JsonSerializerOptions.Web;
    }

    public Task SaveAsync<T>(string key, T draft) =>
        _store.SetAsync(KeyPrefix + key, JsonSerializer.Serialize(draft, _json));

    /// <summary>The saved draft, or default when none (or when the stored shape no longer parses).</summary>
    public async Task<T?> LoadAsync<T>(string key)
    {
        var stored = await _store.GetAsync(KeyPrefix + key);

        if (stored is null)
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(stored, _json);
        }
        catch (JsonException)
        {
            // A draft from an older app version whose shape changed is not worth crashing over.
            await _store.RemoveAsync(KeyPrefix + key);

            return default;
        }
    }

    public Task ClearAsync(string key) => _store.RemoveAsync(KeyPrefix + key);
}