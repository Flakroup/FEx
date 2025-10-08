using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.MVVM.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExMvvmContainer : IContainer<FExMvvm>, IContainer<IMessagePopupService>
{
}