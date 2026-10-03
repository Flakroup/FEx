using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv.Abstractions;

/// <summary>Downloads and disk-caches OneDrive item thumbnails.</summary>
public interface IOneDriveThumbnailService
{
    /// <summary>Gets the <see cref="ThumbnailSize.Medium"/> thumbnail of an item, or <c>null</c> if it has none.</summary>
    Task<byte[]?> GetThumbnailAsync(string itemId, CancellationToken cancellationToken);

    /// <summary>Gets the thumbnail of an item in the requested size, or <c>null</c> if it has none.</summary>
    Task<byte[]?> GetThumbnailAsync(string itemId, ThumbnailSize size, CancellationToken cancellationToken);
}
