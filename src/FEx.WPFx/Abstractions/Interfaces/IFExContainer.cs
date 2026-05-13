using FEx.Common.Abstractions.Interfaces;
using FEx.Json;
using FEx.MVVM.Interfaces;
using FEx.MVVM.Rx.Legacy.Abstractions.Interfaces;
using FEx.Platforms.Abstractions.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace FEx.WPFx.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExContainer : IFExBaseContainer, IFExMvvmContainer, IFExJsonContainer, IFExMvvmRxContainer,
    IFExPlatformsContainer, IFExWpfxContainer
{
}