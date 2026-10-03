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
    Task<T> GetResourceAsync<T>() where T : class, INuGetResource;
}

internal sealed class NuGetOrgResourceSource : INuGetResourceSource
{
    private readonly SourceRepository _sourceRepository =
        NuGetRepository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

    public async Task<T> GetResourceAsync<T>() where T : class, INuGetResource =>
        await _sourceRepository.GetResourceAsync<T>();
}

/// <summary>
/// Creates a value on first use, once, thread-safely. A failed creation is not cached, so a transient
/// network failure does not poison the instance.
/// </summary>
internal sealed class AsyncOnce<T>(Func<Task<T>> factory) where T : class
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    private volatile T? _value;

    public async Task<T> GetAsync()
    {
        if (_value is { } value)
            return value;

        await _gate.WaitAsync();

        try
        {
            return _value ??= await factory();
        }
        finally
        {
            _gate.Release();
        }
    }
}
