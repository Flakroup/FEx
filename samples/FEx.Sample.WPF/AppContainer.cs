using FEx.WPFx;
using FEx.WPFx.Abstractions.Interfaces;
using StrongInject;

namespace FEx.Sample.WPF;

/// <summary>
/// StrongInject container for the WPF application.
/// Demonstrates StrongInject-only Multi-DI pattern (no Microsoft DI).
/// </summary>
[RegisterModule(typeof(FExModule))]
#pragma warning disable IDISP025, SI1105 // IDISP025: StrongInject generated container; SI1105: benign module-resolution warning
public partial class AppContainer : FExModule, IFExContainer
#pragma warning restore IDISP025, SI1105
{
}