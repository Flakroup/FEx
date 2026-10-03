using Microsoft.Graph;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv.Abstractions;

/// <summary>Caches the authenticated Graph client together with the signed-in user's default drive ID.</summary>
public interface IGraphServiceClientCache
{
    /// <summary>Gets the cached client and drive ID, creating them on first use.</summary>
    Task<(GraphServiceClient Client, string DriveId)> GetAsync(CancellationToken cancellationToken);

    /// <summary>Drops the cached client so the next <see cref="GetAsync"/> rebuilds it.</summary>
    void Invalidate();
}