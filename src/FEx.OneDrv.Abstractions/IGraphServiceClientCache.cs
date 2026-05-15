using Microsoft.Graph;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv.Abstractions;

public interface IGraphServiceClientCache
{
    Task<(GraphServiceClient Client, string DriveId)> GetAsync(CancellationToken cancellationToken);

    void Invalidate();
}
