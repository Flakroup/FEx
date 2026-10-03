using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using System;
using System.Threading;
using System.Threading.Tasks;
using NuGetRepository = NuGet.Protocol.Core.Types.Repository;

namespace FEx.NuGetx;

/// <summary>
/// Seam over the remote NuGet source so <see cref="NuGetManager" /> can be exercised without network access.
/// </summary>
internal interface INuGetResourceSource
{
    Task<T> GetResourceAsync<T>(CancellationToken token) where T : class, INuGetResource;
}

internal sealed class NuGetOrgResourceSource : INuGetResourceSource
{
    // The single owner of the nuget.org endpoint inside FEx.NuGetx.
    private const string ServiceIndexUrl = "https://api.nuget.org/v3/index.json";

    private readonly SourceRepository _sourceRepository = NuGetRepository.Factory.GetCoreV3(ServiceIndexUrl);

    public async Task<T> GetResourceAsync<T>(CancellationToken token) where T : class, INuGetResource =>
        await _sourceRepository.GetResourceAsync<T>(token);
}

/// <summary>
/// Creates a value on first use, once, thread-safely. A failed creation is not cached, so a transient
/// network failure or a cancelled caller does not poison the instance.
/// </summary>
internal sealed class AsyncOnce<T>(Func<CancellationToken, Task<T>> factory) where T : class
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    private volatile T? _value;

    public async Task<T> GetAsync(CancellationToken token = default)
    {
        if (_value is { } value)
            return value;

        await _gate.WaitAsync(token);

        try
        {
            if (_value is { } existing)
                return existing;

            var created = await factory(token);
            _value = created;

            return created;
        }
        finally
        {
            _gate.Release();
        }
    }
}
