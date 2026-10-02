using FEx.WPFx.WpfBindingErrors;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FEx.WPFx.Tests;

[Collection(WpfTestCollection.Name)]
public sealed class BindingExceptionThrowerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private string CacheFile => Path.Combine(_dir, "BindingErrors.json");

    public void Dispose()
    {
        BindingExceptionThrower.Detach();

        if (!Directory.Exists(_dir))
            return;

        foreach (var file in Directory.GetFiles(_dir))
            File.SetAttributes(file, FileAttributes.Normal);

        Directory.Delete(_dir, true);
    }

    private static void Report(string message) =>
        BindingExceptionThrower.OnErrorCatched(new(), "System.Windows.Data", TraceEventType.Error, message);

    private void AttachWithReadOnlyCache()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(CacheFile, "[]");
        File.SetAttributes(CacheFile, FileAttributes.ReadOnly);
        BindingExceptionThrower.Attach(_dir);
    }

    [Fact]
    public void Attach_LoadsPersistedCache_AndSuppressesKnownError()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(CacheFile,
            JsonConvert.SerializeObject(new HashSet<BindingException> { new("known error") },
                Formatting.Indented,
                BindingExceptionThrower.DefaultSettings));

        BindingExceptionThrower.Attach(_dir);

        BindingExceptionThrower.BindingErrorsCacheFile.ShouldBe(CacheFile);
        Should.NotThrow(() => Report("known error"));
    }

    [Fact]
    public void Attach_NewError_ThrowsOnceAndIsPersistedToStableFile()
    {
        BindingExceptionThrower.Attach(_dir);

        Should.Throw<BindingException>(() => Report("new error")).Message.ShouldBe("new error");
        Should.NotThrow(() => Report("new error"));

        // A later run (same directory) loads the error back instead of reporting it again.
        BindingExceptionThrower.Attach(_dir);
        Should.NotThrow(() => Report("new error"));
    }

    [Fact]
    public void Attach_WithoutDirectory_DoesNotPersist()
    {
        BindingExceptionThrower.Attach(null);

        BindingExceptionThrower.BindingErrorsCacheFile.ShouldBeNull();
        Should.Throw<BindingException>(() => Report("in memory error"));
        Should.NotThrow(() => Report("in memory error"));
    }

    [Fact]
    public async Task OnErrorCatched_UnwritableCache_ThrowsBindingExceptionAndReleasesSemaphore()
    {
        AttachWithReadOnlyCache();

        Should.Throw<BindingException>(() => Report("error A")).Message.ShouldBe("error A");

        var second = Task.Run(() => Should.Throw<BindingException>(() => Report("error B")));

        (await Task.WhenAny(second, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken))).ShouldBe(second);
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
    public void LoadCachedBindingErrors_UnreadableFile_ReturnsEmpty()
    {
        Directory.CreateDirectory(_dir);
        using var locked = new FileStream(CacheFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        BindingExceptionThrower.LoadCachedBindingErrors(CacheFile).ShouldBeEmpty();
    }

    [Fact]
    public void LoadCachedBindingErrors_CorruptFile_ReturnsEmpty()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(CacheFile, "{ not json");

        BindingExceptionThrower.LoadCachedBindingErrors(CacheFile).ShouldBeEmpty();
    }
}
