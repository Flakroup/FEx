using System.Threading;
using System.Threading.Tasks;

namespace FEx.Common.Abstractions.Interfaces;

public interface IDeviceHelper
{
    /// <summary>
    /// True if the device is currently connected to the internet.
    /// </summary>
    bool HasInternet { get; }

    /// <summary>
    /// Waits for a device to be connected to internet, or returns straight away if it already is.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True when it is connected, false in case of any exception</returns>
    ValueTask<bool> WaitForInternetAsync(CancellationToken cancellationToken);
}