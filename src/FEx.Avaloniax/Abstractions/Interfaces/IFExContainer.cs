using FEx.Common.Abstractions.Interfaces;
using FEx.Json;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Rx.Abstractions.Interfaces;
using FEx.Platforms.Abstractions.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Avaloniax.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExContainer : IFExBaseContainer, IFExMvvmContainer, IFExJsonContainer, IFExMvvmRxContainer,
    IFExPlatformsContainer, IFExAvaloniaContainer
{
}