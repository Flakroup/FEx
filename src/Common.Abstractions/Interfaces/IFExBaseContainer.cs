using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Common.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExBaseContainer : IFExCommonContainer, IFExDependencyInjectionContainer, IFExCoreContainer,
    IFExLoggingContainer
{
}