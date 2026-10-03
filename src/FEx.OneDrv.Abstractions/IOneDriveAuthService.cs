using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv.Abstractions;

/// <summary>Acquires and clears OneDrive (Microsoft Graph) access tokens.</summary>
public interface IOneDriveAuthService
{
    /// <summary>Gets an access token, silently when possible and interactively otherwise.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
    /// <summary>Removes all cached accounts.</summary>
    Task SignOutAsync(CancellationToken cancellationToken);
}