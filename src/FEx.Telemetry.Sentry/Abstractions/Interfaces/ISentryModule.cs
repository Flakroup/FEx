using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Telemetry.Sentry.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface ISentryModule : IFExTelemetryModule, IContainer<ISentryService>, IContainer<FExSentryx>
{
}
