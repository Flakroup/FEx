using System;
using System.IO;
using System.Text.Json;

namespace FEx.WPFx.Fluent.Shell;

/// <summary>
/// What the shell remembers between runs: the open module and the manual menu state. The app keeps it in a file
/// of its own, apart from its settings file: a settings loader that answers an unreadable file with an empty
/// configuration would otherwise overwrite the connection when the shell saved its state through it.
/// </summary>
public sealed record ShellState(string LastModule, string MenuOverride)
{
    public static readonly ShellState Empty = new(string.Empty, string.Empty);

    /// <summary>The stored state, or <see cref="Empty"/> when the file is missing or unreadable - remembering is a convenience.</summary>
    public static ShellState Load(string path)
    {
        try
        {
            return File.Exists(path) && JsonSerializer.Deserialize<ShellState>(File.ReadAllText(path)) is { } state
                ? new ShellState(state.LastModule ?? string.Empty, state.MenuOverride ?? string.Empty) // a file may omit or null either
                : Empty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return Empty;
        }
    }

    /// <summary>Best effort: a read-only install folder must not break navigation.</summary>
    public void Save(string path)
    {
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(this));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
