using FEx.MVVM.Abstractions.Interfaces;
using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.MVVM.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExMvvmContainer : IContainer<FExMvvm>, IContainer<IMessagePopupService>
{
}