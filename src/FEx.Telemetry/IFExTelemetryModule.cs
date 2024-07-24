using FEx.Telemetry.Subjects;
using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Telemetry;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExTelemetryModule : IContainer<TelemetryAccessTokenSubject>, IContainer<IFExTelemetryConfig>
{
}