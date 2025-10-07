using FEx.Fundamentals;
using FEx.Json;
using FEx.Logging.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Rx.Abstractions.Interfaces;
using FEx.Platforms.Abstractions.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace FEx.WPFx.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExContainer : IFExFundamentalsContainer, IFExMvvmContainer, IFExJsonContainer, IFExLoggingContainer,
    IFExMvvmRxContainer, IFExPlatformsContainer, IFExWpfxContainer
{
}