using FEx.WPFx;
using FEx.WPFx.Abstractions.Interfaces;
using StrongInject;

namespace FEx.Sample.WPF;

/// <summary>
/// StrongInject container for the WPF application.
/// Demonstrates StrongInject-only Multi-DI pattern (no Microsoft DI).
/// </summary>
[RegisterModule(typeof(FExModule))]
public partial class AppContainer : FExModule, IFExContainer
{
}