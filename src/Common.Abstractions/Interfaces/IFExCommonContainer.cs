using FEx.Core.Abstractions.Interfaces;
using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Common.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExCommonContainer : IContainer<IMainThreadContextProvider>, IContainer<IFExInternetConnectionHelper>,
    IContainer<IDeviceHelper>, IContainer<IConnectivityChangedSubject>
{
}