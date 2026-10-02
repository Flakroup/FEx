namespace FEx.WPFx.Tests;

/// <summary>
/// WPF tests share process-global state (the binding-error trace listener, <c>FExCoreStatics</c> factories),
/// so they run serially.
/// </summary>
internal static class WpfTestCollection
{
    public const string Name = "WPF global state";
}
