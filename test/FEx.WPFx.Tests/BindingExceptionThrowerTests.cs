using FEx.WPFx.WpfBindingErrors;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace FEx.WPFx.Tests;

public sealed class BindingExceptionThrowerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private string CacheFile => Path.Combine(_dir, "BindingErrors.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    [Fact]
    public void LoadCachedBindingErrors_MissingFile_ReturnsEmptyAndCreatesDirectory()
    {
        BindingExceptionThrower.LoadCachedBindingErrors(CacheFile).ShouldBeEmpty();

        Directory.Exists(_dir).ShouldBeTrue();
    }

    [Fact]
    public void LoadCachedBindingErrors_PersistedFile_IsLoadedBack()
    {
        Directory.CreateDirectory(_dir);
        var persisted = new HashSet<BindingException> { new("Cannot find source") };
        File.WriteAllText(CacheFile,
            JsonConvert.SerializeObject(persisted, Formatting.Indented, BindingExceptionThrower.DefaultSettings));

        var loaded = BindingExceptionThrower.LoadCachedBindingErrors(CacheFile);

        loaded.ShouldHaveSingleItem().Message.ShouldBe("Cannot find source");
    }

    [Fact]
    public void LoadCachedBindingErrors_CorruptFile_ReturnsEmpty()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(CacheFile, "{ not json");

        BindingExceptionThrower.LoadCachedBindingErrors(CacheFile).ShouldBeEmpty();
    }
}
