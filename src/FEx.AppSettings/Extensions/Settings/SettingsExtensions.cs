using System.Configuration;

namespace FEx.AppSettings.Extensions.Settings;

public static class SettingsExtensions
{
    /// <summary>
    ///     Saves the and reload settings.
    /// </summary>
    /// <param name="settings">The settings.</param>
    public static void SaveAndReloadSettings(this ApplicationSettingsBase settings)
    {
        settings.Save();
        settings.Reload();
    }
}