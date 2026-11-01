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

    [Theory]
    [InlineData("{}", "", "")]
    [InlineData("{\"LastModule\":\"send\"}", "send", "")]
    [InlineData("{\"MenuOverride\":\"Rail\"}", "", "Rail")]
    [InlineData("{\"LastModule\":null,\"MenuOverride\":null}", "", "")]
    [InlineData("null", "", "")]
    public void AFileThatOmitsOrNullsAProperty_LoadsThemAsEmptyStrings(string json, string module, string menu)
    {
        var path = TempPath();
        File.WriteAllText(path, json);
        try
        {
            var state = ShellState.Load(path);

            state.LastModule.ShouldBe(module);
            state.MenuOverride.ShouldBe(menu);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SaveToAReadOnlyFile_DoesNotThrow_AndLeavesItAlone()
    {
        var path = TempPath();
        File.WriteAllText(path, "kept");
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            Should.NotThrow(() => new ShellState("send", "Rail").Save(path));

            File.ReadAllText(path).ShouldBe("kept");
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }
    }

    [Fact]
    public void SaveToAnUnwritableLocation_DoesNotThrow() =>
        Should.NotThrow(() => new ShellState("send", string.Empty)
            .Save(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing-folder", "x.json")));
}
