using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv.Abstractions;

public interface IOneDriveAuthService
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
    Task SignOutAsync(CancellationToken cancellationToken);
}
