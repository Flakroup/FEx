using FEx.Agnostics.Abstractions.Flow;
using FEx.Common.Abstractions.Enums;
using FEx.Common.Abstractions.Interfaces;
using FEx.Core.Abstractions.Extensions;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Common.Helpers;

public class DeviceHelper : IDeviceHelper
{
    private readonly IConnectivityChangedSubject _connectivityChangedSubject;

    /// <inheritdoc />
    public bool HasInternet => _connectivityChangedSubject.Value == FExNetworkAccess.Internet;

    public DeviceHelper(IConnectivityChangedSubject connectivityChangedSubject)
    {
        _connectivityChangedSubject = connectivityChangedSubject;
    }

    /// <inheritdoc />
    public async ValueTask<bool> WaitForInternetAsync(CancellationToken cancellationToken = default)
    {
        if (HasInternet)
            return true;

        Result<bool, Error> result = await _connectivityChangedSubject
            .Select(static networkAccess => networkAccess == FExNetworkAccess.Internet)
            .GetResultAsync(cancellationToken);

        return result.Data;
    }
}