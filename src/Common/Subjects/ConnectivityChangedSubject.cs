using FEx.Common.Abstractions.Enums;
using FEx.Common.Abstractions.Interfaces;
using FEx.Core.Abstractions.Subjects;

namespace FEx.Common.Subjects;

public class ConnectivityChangedSubject : FExBehaviorSubject<FExNetworkAccess>, IConnectivityChangedSubject
{
    public ConnectivityChangedSubject()
        : this(FExNetworkAccess.Unknown)
    {
    }

    protected ConnectivityChangedSubject(FExNetworkAccess networkAccess)
        : base(networkAccess)
    {
    }
}