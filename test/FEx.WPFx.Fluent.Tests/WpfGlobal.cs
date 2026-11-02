namespace FEx.WPFx.Fluent.Tests;

/// <summary>
/// Groups every test that builds WPF objects. They share process-global state - the one <c>Application</c> a process
/// may create, pack:// loading, the resource cache - so they run serially.
/// </summary>
public static class WpfGlobal
{
    public const string Collection = "WPF global state";
}
