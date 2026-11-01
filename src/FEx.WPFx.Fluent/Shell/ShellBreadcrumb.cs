using System.Collections.Generic;

namespace FEx.WPFx.Fluent.Shell;

/// <summary>The breadcrumb in the context bar: "{app} / {group} / {module}" when there is room, the module alone when not.</summary>
public static class ShellBreadcrumb
{
    /// <summary>The segments to draw, separators excluded; an entry without a group (Ustawienia) skips that segment.</summary>
    /// <param name="appName">The application's display name, the first segment of the full breadcrumb.</param>
    /// <param name="module">The entry the operator is on, the last segment.</param>
    /// <param name="compact">True when there is room for the module alone.</param>
    public static IReadOnlyList<string> Segments(string appName, ShellModule module, bool compact)
    {
        if (compact)
            return [module.Label];

        return module.GroupLabel.Length == 0
            ? [appName, module.Label]
            : [appName, module.GroupLabel, module.Label];
    }
}
