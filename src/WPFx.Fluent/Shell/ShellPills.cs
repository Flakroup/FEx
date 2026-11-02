namespace FEx.WPFx.Fluent.Shell;

public enum PillSeverity
{
    Ok,
    Warning,
    Danger,
}

/// <summary>What a status pill in the context bar shows: its text and, through a severity, its colours.</summary>
public sealed record PillState(PillSeverity Severity, string Text)
{
    /// <summary>Theme token keys - the shell paints from these, never from a literal.</summary>
    public string BackgroundKey => Severity switch
    {
        PillSeverity.Ok => "OkSoft",
        PillSeverity.Warning => "WarnSoft",
        _ => "DangerSoft",
    };

    public string InkKey => Severity switch
    {
        PillSeverity.Ok => "Ok",
        PillSeverity.Warning => "WarnInk",
        _ => "Danger",
    };

    public string DotKey => Severity switch
    {
        PillSeverity.Ok => "Ok",
        PillSeverity.Warning => "WarnBar",
        _ => "Danger",
    };
}

/// <summary>The pill states every shell shares, from raw facts.</summary>
public static class ShellPills
{
    public static PillState Database(bool configured, bool reachable) =>
        !configured ? new(PillSeverity.Danger, "Brak konfiguracji bazy")
        : !reachable ? new(PillSeverity.Danger, "Brak połączenia z bazą")
        : new(PillSeverity.Ok, "Baza połączona");

    /// <summary>A pill click opens Ustawienia only while there is something to fix there.</summary>
    public static bool DatabaseOpensSettings(PillState state) => state.Severity != PillSeverity.Ok;
}
