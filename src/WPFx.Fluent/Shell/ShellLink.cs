using System.Windows;

namespace FEx.WPFx.Fluent.Shell;

/// <summary>Carries whether the data source answered, up the tree to whoever shows the connection status.</summary>
public sealed class DatabaseReachabilityEventArgs(RoutedEvent routedEvent, object source, bool reachable)
    : RoutedEventArgs(routedEvent, source)
{
    public bool Reachable { get; } = reachable;
}

public delegate void DatabaseReachabilityEventHandler(object sender, DatabaseReachabilityEventArgs e);

/// <summary>
/// How a tab talks to the shell around it: two bubbling routed events. A window (or any ancestor) that handles them
/// reacts; one that does not is unaffected, so a view hosted without a shell - every test - simply has nobody to tell.
/// </summary>
public static class ShellLink
{
    /// <summary>A tab asks the shell to open its settings.</summary>
    public static readonly RoutedEvent OpenSettingsEvent = EventManager.RegisterRoutedEvent(
        "OpenSettings", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ShellLink));

    /// <summary>A tab reports that its data source answered (<see cref="DatabaseReachabilityEventArgs.Reachable"/> true) or did not.</summary>
    public static readonly RoutedEvent DatabaseReachabilityEvent = EventManager.RegisterRoutedEvent(
        "DatabaseReachability", RoutingStrategy.Bubble, typeof(DatabaseReachabilityEventHandler), typeof(ShellLink));

    public static void RaiseOpenSettings(UIElement from) =>
        from.RaiseEvent(new RoutedEventArgs(OpenSettingsEvent, from));

    public static void RaiseDatabaseReachability(UIElement from, bool reachable) =>
        from.RaiseEvent(new DatabaseReachabilityEventArgs(DatabaseReachabilityEvent, from, reachable));
}
