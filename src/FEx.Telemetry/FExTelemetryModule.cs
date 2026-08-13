using FEx.Telemetry.Subjects;
using StrongInject;

namespace FEx.Telemetry;

[Register(typeof(TelemetryAccessTokenSubject), Scope.SingleInstance)]
public class FExTelemetryModule
{
}