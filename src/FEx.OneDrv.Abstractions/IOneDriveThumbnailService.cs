using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv.Abstractions;

public interface IOneDriveThumbnailService
{
    Task<byte[]?> GetThumbnailAsync(string itemId, CancellationToken cancellationToken);
}