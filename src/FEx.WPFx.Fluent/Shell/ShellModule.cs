using System.Collections.Generic;
using System.Linq;

namespace FEx.WPFx.Fluent.Shell;

/// <summary>One entry of the side menu: a module the operator can open, or one still in preparation.</summary>
/// <param name="Key">Stable identifier; stored in the settings as the last opened module.</param>
/// <param name="GroupLabel">Menu group header; empty for the entry pinned to the bottom.</param>
/// <param name="Label">Menu text, also the last breadcrumb segment.</param>
/// <param name="Glyph">Segoe Fluent Icons / MDL2 code point.</param>
/// <param name="InPreparation">Greyed out and not openable: the module is not built yet.</param>
public sealed record ShellModule(string Key, string GroupLabel, string Label, string Glyph, bool InPreparation = false)
{
    private const string ClockGlyph = "";

    public bool IsAvailable => !InPreparation;

    /// <summary>Clock icon for a module in preparation, its own icon otherwise.</summary>
    public string DisplayGlyph => InPreparation ? ClockGlyph : Glyph;

    /// <summary>What the hover tip says: the label (it is the only text on the icon rail) or why the entry is dead.</summary>
    public string Tooltip => InPreparation ? $"{Label} - moduł w przygotowaniu" : Label;
}

