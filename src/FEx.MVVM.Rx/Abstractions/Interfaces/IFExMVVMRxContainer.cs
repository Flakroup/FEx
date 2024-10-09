using FEx.Abstractions.Interfaces;
using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.MVVM.Rx.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExMvvmRxContainer : IContainer<FExMvvmRx>, IContainer<IStatusService>
{
}