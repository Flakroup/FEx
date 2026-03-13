using FEx.Common.Abstractions.Enums;
using FEx.Core.Abstractions.Interfaces;

namespace FEx.Common.Abstractions.Interfaces;

public interface IConnectivityChangedSubject : IFExBehaviorSubject<FExNetworkAccess>
{
}