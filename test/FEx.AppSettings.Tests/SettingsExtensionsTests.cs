using FEx.AppSettings.Extensions.Settings;
using Shouldly;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using Xunit;

namespace FEx.AppSettings.Tests;

/// <summary>SaveAndReloadSettings persists the pending changes first and then reloads from the provider.</summary>
public sealed class SettingsExtensionsTests
{
    [Fact]
    public void SaveAndReloadSettings_PersistsTheChange_ThenReloadsItFromTheProvider()
    {
        InMemorySettingsProvider.Store.Clear();
        MemorySettings settings = new();
        settings.Name.ShouldBe("default");

        settings.Name = "changed";
        InMemorySettingsProvider.Store.ShouldBeEmpty();

        settings.SaveAndReloadSettings();

        InMemorySettingsProvider.Store.ShouldContainKeyAndValue(nameof(MemorySettings.Name), "changed");
        settings.Name.ShouldBe("changed");
    }

    [Fact]
    public void SaveAndReloadSettings_DropsUnsavedChangesOfAnotherInstance_ByReadingTheStoredValue()
    {
        InMemorySettingsProvider.Store[nameof(MemorySettings.Name)] = "stored";
        MemorySettings settings = new();

        settings.SaveAndReloadSettings();

        settings.Name.ShouldBe("stored");
    }

    [SettingsProvider(typeof(InMemorySettingsProvider))]
    private sealed class MemorySettings : ApplicationSettingsBase
    {
        [UserScopedSetting]
        [DefaultSettingValue("default")]
        public string Name
        {
            get => (string)this[nameof(Name)];
            set => this[nameof(Name)] = value;
        }
    }

    public sealed class InMemorySettingsProvider : SettingsProvider
    {
        public static Dictionary<string, object?> Store { get; } = [];

        public override string ApplicationName { get; set; } = nameof(InMemorySettingsProvider);

        public override void Initialize(string? name, NameValueCollection? config) =>
            base.Initialize(name ?? nameof(InMemorySettingsProvider), config ?? []);

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context,
                                                                          SettingsPropertyCollection collection)
        {
            SettingsPropertyValueCollection values = [];

            foreach (SettingsProperty property in collection)
            {
                SettingsPropertyValue value = new(property);

                if (Store.TryGetValue(property.Name, out var stored))
                {
                    value.PropertyValue = stored;
                    value.IsDirty = false;
                }

                values.Add(value);
            }

            return values;
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
        {
            foreach (SettingsPropertyValue value in collection)
                Store[value.Name] = value.PropertyValue;
        }
    }
}
