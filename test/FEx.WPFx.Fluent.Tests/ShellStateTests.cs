using FEx.WPFx.Fluent.Shell;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

public sealed class ShellStateTests
{
    private static string TempPath() => Path.Combine(Path.GetTempPath(), $"fex-fluent-shell-{Guid.NewGuid():N}.json");

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var path = TempPath();
        try
        {
            new ShellState("send", "Rail").Save(path);

            ShellState.Load(path).ShouldBe(new ShellState("send", "Rail"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingFile_LoadsAsEmpty() => ShellState.Load(TempPath()).ShouldBe(ShellState.Empty);

    [Fact]
    public void UnreadableFile_LoadsAsEmpty_InsteadOfBreakingStartup()
    {
        var path = TempPath();
        File.WriteAllText(path, "{ not json");
        try
        {
            ShellState.Load(path).ShouldBe(ShellState.Empty);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SaveToAnUnwritableLocation_DoesNotThrow() =>
        Should.NotThrow(() => new ShellState("send", string.Empty)
            .Save(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing-folder", "x.json")));
}
