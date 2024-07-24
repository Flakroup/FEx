using FEx.Basics.Abstractions;
using FEx.Common.Extensions;
using FEx.Common.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace FEx.Telemetry;

public abstract class FExTelemetryConfigBase : NotifyPropertyChanged, IFExTelemetryConfig
{
    private string _accessToken;

    public string AccessToken
    {
        get => _accessToken;
        set => SetProperty(ref _accessToken, value.Guard(nameof(AccessToken)), OnAccessTokenChanged);
    }

    public string AppEnvironment => GetAppEnvironment();
    public Func<string> PersonEmail { get; set; }
    public Func<string> PersonUserName { get; set; }
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
        string userName = AddPersonToEnvironment
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