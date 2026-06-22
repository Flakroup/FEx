using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Telemetry.Sentry.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExSentryModule : IFExTelemetryModule, IContainer<ISentryService>, IContainer<FExSentryx>, IContainer<FExSentryConfig>
{
}
