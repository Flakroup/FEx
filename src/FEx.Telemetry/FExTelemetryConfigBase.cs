using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Agnostics.BaseObjects;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace FEx.Telemetry;

public abstract class FExTelemetryConfigBase : NotifyPropertyChanged, IFExTelemetryConfig
{
    // Init-before-use: AccessToken setter Guards non-null; a valid config must assign it before AccessToken is read.
    private string _accessToken = null!;

    public string AccessToken
    {
        get => _accessToken;
        set => SetProperty(ref _accessToken, value.Guard(nameof(value)), OnAccessTokenChanged);
    }

    public string AppEnvironment => GetAppEnvironment();
    public Func<string>? PersonEmail { get; set; }
    public Func<string>? PersonUserName { get; set; }
    public bool AddPersonToEnvironment { get; set; }

    protected string EnvironmentParam { get; }

    protected FExTelemetryConfigBase()
    {
        EnvironmentParam = Debugger.IsAttached
            ? "development"
            : "production";
    }

    protected abstract void OnAccessTokenChanged(string accessToken);

    private string GetAppEnvironment()
    {
        var userName = AddPersonToEnvironment
            ? PersonUserName?.Invoke()
            : null;

        var appEnvironment = new StringBuilder();

        if (userName is not null)
            appEnvironment.Append($"{userName}_");

        appEnvironment.Append(
            $"{EnvironmentParam} ~ {PlatformInfoProvider.InfoString} ~ {CultureInfo.InstalledUICulture.EnglishName}");

        return appEnvironment.ToString();
    }
}