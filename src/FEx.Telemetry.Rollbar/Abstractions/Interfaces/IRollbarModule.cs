using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Telemetry.Rollbar.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IRollbarModule : IFExTelemetryModule, IContainer<IRollbarService>, IContainer<IRollbarConfig>,
    IContainer<FExRollbarx>
{
}