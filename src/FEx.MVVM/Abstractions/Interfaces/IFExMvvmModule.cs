using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.MVVM.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExMvvmModule : IContainer<FExMvvm>, IContainer<IMessagePopupService>
{
}