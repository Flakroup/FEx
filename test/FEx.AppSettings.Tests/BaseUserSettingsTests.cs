using FEx.AppSettings.Abstractions;
using FEx.AppSettings.Abstractions.Interfaces;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FEx.AppSettings.Tests;

/// <summary>User settings: loaded from a JSON file, saved on every change, with a created hook for first runs.</summary>
public sealed class BaseUserSettingsTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private static async Task<string> WaitForFileContainingAsync(string path, string expected)
    {
        var watch = Stopwatch.StartNew();

        while (watch.Elapsed < TimeSpan.FromSeconds(10))
        {
            try
            {
                if (File.Exists(path))
                {
                    var text = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);

                    if (text.Contains(expected, StringComparison.Ordinal))
                        return text;
                }
            }
            catch (IOException)
            {
                // the background save still holds the file; try again
            }

            await Task.Delay(10);
        }

        throw new TimeoutException($"'{path}' never contained '{expected}'.");
    }

    [Fact]
    public void GetSettings_WithoutAFile_CreatesDefaultsAndRaisesCreated()
    {
        var settings = TestSettings.GetSettings(_dir.File("user.json"));

        settings.Name.ShouldBeNull();
        settings.CreatedCalls.ShouldBe(1);
        settings.PersistencePath.ShouldBe(_dir.File("user.json"));
        settings.IsAsync.ShouldBeFalse();
    }

    [Fact]
    public void GetSettings_WhenTheTypeDoesNotOverrideOnCreated_StillWorks()
    {
        var settings = PlainSettings.GetSettings(_dir.File("plain.json"));

        settings.Title = "t";

        PlainSettings.GetSettings(_dir.File("plain.json")).Title.ShouldBe("t");
    }

    [Fact]
    public void GetSettings_WithoutAPath_WorksInMemoryAndNeverWritesAFile()
    {
        var settings = TestSettings.GetSettings();

        settings.PersistencePath.ShouldBeNull();
        settings.CreatedCalls.ShouldBe(1);

        settings.Name = "in memory";
        settings.SaveSettings();

        settings.Name.ShouldBe("in memory");
    }

    [Fact]
    public void GetSettings_WithAnEmptyPath_IsTreatedAsNoPersistence()
    {
        var settings = TestSettings.GetSettings("");

        settings.Name = "x";

        settings.Name.ShouldBe("x");
        settings.CreatedCalls.ShouldBe(1);
    }

    [Fact]
    public void GetSettings_CreatesTheMissingDirectoryForTheFile()
    {
        var path = Path.Combine(_dir.Path, "a", "b", "user.json");

        TestSettings.GetSettings(path);

        Directory.Exists(Path.Combine(_dir.Path, "a", "b")).ShouldBeTrue();
    }

    [Fact]
    public void GetSettings_ReadsAnExistingFile_AndDoesNotRaiseCreated()
    {
        var path = _dir.File("user.json");
        File.WriteAllText(path, """{ "Name": "stored", "Count": 7 }""");

        var settings = TestSettings.GetSettings(path);

        settings.Name.ShouldBe("stored");
        settings.Count.ShouldBe(7);
        settings.CreatedCalls.ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    public void GetSettings_ForAnEmptyOrNullDocument_FallsBackToDefaults(string content)
    {
        var path = _dir.File("user.json");
        File.WriteAllText(path, content);

        var settings = TestSettings.GetSettings(path);

        settings.Name.ShouldBeNull();
        settings.Count.ShouldBe(0);
    }

    [Fact]
    public void GetSettings_WithTheAsyncFlag_RemembersIt()
    {
        TestSettings.GetSettings(_dir.File("user.json"), true).IsAsync.ShouldBeTrue();
        TestSettings.GetSettings(_dir.File("user2.json"), false).IsAsync.ShouldBeFalse();
    }

    [Fact]
    public void ChangingAProperty_SavesTheFileImmediately_WhenSynchronous()
    {
        var path = _dir.File("user.json");
        var settings = TestSettings.GetSettings(path);

        settings.Name = "Jan";

        var json = File.ReadAllText(path);
        json.ShouldContain("\"Name\": \"Jan\"");
        json.ShouldNotContain("PersistencePath");
        json.ShouldNotContain("IsAsync");
    }

    [Fact]
    public void ChangedSettings_AreReadBackByTheNextGetSettings()
    {
        var path = _dir.File("user.json");
        var settings = TestSettings.GetSettings(path);
        settings.Name = "Jan";
        settings.Count = 3;

        var reloaded = TestSettings.GetSettings(path);

        reloaded.Name.ShouldBe("Jan");
        reloaded.Count.ShouldBe(3);
        reloaded.CreatedCalls.ShouldBe(0);
    }

    [Fact]
    public void SettingTheSameValueAgain_DoesNotRewriteTheFile()
    {
        var path = _dir.File("user.json");
        var settings = TestSettings.GetSettings(path);
        settings.Name = "Jan";
        File.Delete(path);

        settings.Name = "Jan";

        File.Exists(path).ShouldBeFalse();
    }

    [Fact]
    public void ChangingAProperty_RaisesPropertyChangedAndTheCallback_AfterSaving()
    {
        var path = _dir.File("user.json");
        var settings = TestSettings.GetSettings(path);
        List<string?> raised = [];
        settings.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        settings.NameWithCallback = "cb";

        raised.ShouldContain(nameof(TestSettings.NameWithCallback));
        settings.CallbackValues.ShouldBe(["cb"]);
        File.ReadAllText(path).ShouldContain("cb");
    }

    [Fact]
    public void ChangingAProperty_WithoutAPath_StillRunsTheCallback()
    {
        var settings = TestSettings.GetSettings();

        settings.NameWithCallback = "cb";

        settings.CallbackValues.ShouldBe(["cb"]);
    }

    [Fact]
    public async Task ChangingAProperty_SavesInTheBackground_WhenAsync()
    {
        var path = _dir.File("user.json");
        var settings = TestSettings.GetSettings(path, true);

        settings.Name = "background";

        var json = await WaitForFileContainingAsync(path, "background");
        json.ShouldContain("\"Name\": \"background\"");
    }

    [Fact]
    public async Task SaveSettingsAsync_WritesTheCurrentState()
    {
        var path = _dir.File("user.json");
        var settings = TestSettings.GetSettings(path);
        settings.Name = "first";
        File.Delete(path);

        await settings.SaveSettingsAsync();

        (await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).ShouldContain("\"Name\": \"first\"");
    }

    [Fact]
    public void SaveSettings_WithoutAPath_DoesNothing()
    {
        var settings = TestSettings.GetSettings();

        Should.NotThrow(settings.SaveSettings);
    }

    [Fact]
    public async Task SaveSettingsAsync_WithoutAPath_DoesNothing()
    {
        var settings = TestSettings.GetSettings();

        await Should.NotThrowAsync(settings.SaveSettingsAsync);
    }

    [Fact]
    public void SaveSettings_AfterAFailedWrite_ReleasesTheLock_SoTheNextSaveWorks()
    {
        var path = _dir.File("user.json");
        var settings = TestSettings.GetSettings(path);
        File.Delete(path);
        Directory.CreateDirectory(path);

        Should.Throw<Exception>(settings.SaveSettings);

        Directory.Delete(path);
        settings.SaveSettings();
        File.Exists(path).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveSettingsAsync_AfterAFailedWrite_ReleasesTheLock_SoTheNextSaveWorks()
    {
        var path = _dir.File("user.json");
        var settings = TestSettings.GetSettings(path);
        File.Delete(path);
        Directory.CreateDirectory(path);

        await Should.ThrowAsync<Exception>(settings.SaveSettingsAsync);

        Directory.Delete(path);
        await settings.SaveSettingsAsync();
        File.Exists(path).ShouldBeTrue();
    }

    [Fact]
    public void Initialize_WithANewPath_MovesFutureSavesThere()
    {
        var oldPath = _dir.File("old.json");
        var newPath = Path.Combine(_dir.Path, "new", "user.json");
        var settings = TestSettings.GetSettings(oldPath);
        settings.Name = "before";
        File.Delete(oldPath);

        settings.Initialize(newPath, (true, false));
        settings.Name = "after";

        settings.PersistencePath.ShouldBe(newPath);
        File.Exists(oldPath).ShouldBeFalse();
        File.ReadAllText(newPath).ShouldContain("after");
        settings.CreatedCalls.ShouldBe(1);
    }

    [Fact]
    public void Initialize_ToANullPath_StopsPersisting()
    {
        var path = _dir.File("user.json");
        var settings = TestSettings.GetSettings(path);
        File.Delete(path);

        settings.Initialize(null, (true, false));
        settings.Name = "x";

        settings.PersistencePath.ShouldBeNull();
        File.Exists(path).ShouldBeFalse();
    }

    [Fact]
    public void PersistencePath_RaisesPropertyChangedWhenItChanges()
    {
        var settings = TestSettings.GetSettings(_dir.File("one.json"));
        List<string?> raised = [];
        settings.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        settings.Initialize(_dir.File("two.json"), (true, false));

        raised.ShouldContain(nameof(IBaseUserSettings.PersistencePath));
    }

    [Fact]
    public void AppTheme_HasLightAndDark()
    {
        Enum.GetNames<AppTheme>().ShouldBe(["Light", "Dark"]);
    }

    private sealed class PlainSettings : BaseUserSettings<PlainSettings>
    {
        private string? _title;

        public string? Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
    }

    private sealed class TestSettings : BaseUserSettings<TestSettings>
    {
        private string? _name;
        private int _count;
        private string? _nameWithCallback;

        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }

        public string? NameWithCallback
        {
            get => _nameWithCallback;
            set => SetProperty(ref _nameWithCallback, value, CallbackValues.Add);
        }

        [JsonIgnore]
        public List<string?> CallbackValues { get; } = [];

        [JsonIgnore]
        public int CreatedCalls { get; private set; }

        protected override void OnCreated() => CreatedCalls++;
    }
}
