using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Extensions;
using FEx.Platforms;
using FEx.Telemetry.Rollbar.Abstractions.Interfaces;
using FEx.Telemetry.Subjects;
using Rollbar;
using Rollbar.Common;
using Rollbar.DTOs;
using Rollbar.PayloadStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace FEx.Telemetry.Rollbar.Configuration;

public abstract class RollbarConfigBase : FExTelemetryConfigBase, IRollbarConfig, IDisposable
{
    private readonly CompositeDisposable _disposable;
    private readonly IAppInfoProvider _appInfoProvider;
    private DirectoryInfo _localAppDataDir;
    private FileInfo _defaultRollbarStoreDbFile;
    private RollbarInfrastructureConfig _rollbarConfig;

    public DirectoryInfo LocalAppDataDir
    {
        get => _localAppDataDir;
        protected set
        {
            _localAppDataDir = value;
            OnLocalAppDataDirChanged(value);
        }
    }

    public FileInfo DefaultRollbarStoreDbFile
    {
        get => _defaultRollbarStoreDbFile;
        protected set
        {
            _defaultRollbarStoreDbFile = value;
            PayloadStoreConstants.DefaultRollbarStoreDbFile = value.FullName;
        }
    }

    public Version AppVersion => FExCoreStatics.AppInfoProvider.Version;

    public bool IsConfigured { get; private set; }

    public RollbarInfrastructureConfig RollbarConfig =>
        _rollbarConfig is null
        || _rollbarConfig.RollbarLoggerConfig.RollbarDestinationOptions.AccessToken != AccessToken
            ? _rollbarConfig = GetRollbarConfig()
            : _rollbarConfig;

    private static string DefaultID { get; } = $"{Environment.UserName}@{Environment.MachineName}";

    protected RollbarConfigBase(TelemetryAccessTokenSubject telemetryAccessTokenSubject,
                                IAppInfoProvider appInfoProvider,
                                string accessToken)
    {
        _appInfoProvider = appInfoProvider;
        _disposable = [];

        telemetryAccessTokenSubject.Where(token => token is not null)
            .DistinctUntilChanged()
            .Subscribe(token => AccessToken = token)
            .DisposeUsing(_disposable);

        telemetryAccessTokenSubject.OnNext(accessToken);
    }

    protected virtual RollbarInfrastructureConfig GetRollbarConfig()
    {
        var token = AccessToken.Guard(nameof(AccessToken), "Token cannot be null");
        var appPerson = GetAppPerson();
        var config = new RollbarInfrastructureConfig(token, AppEnvironment);

        var payloadAdditionOptions = new RollbarPayloadAdditionOptions
        {
            Person = appPerson
        };

        config.RollbarLoggerConfig.RollbarPayloadAdditionOptions.Reconfigure(payloadAdditionOptions);

        var payloadManipulationOptions = new RollbarPayloadManipulationOptions
        {
            Transform = payload =>
            {
                payload.Data.Person = appPerson;
                payload.Data.CodeVersion = AppVersion.ToString();

                payload.Data.Request = new()
                {
                    UserIp = "$remote_ip"
                };
            }
        };

        config.RollbarLoggerConfig.RollbarPayloadManipulationOptions.Reconfigure(payloadManipulationOptions);

        return Validate(config);
    }

    protected virtual void OnLocalAppDataDirChanged(DirectoryInfo value)
    {
        const string rollbarPayloadsStoreDb = "RollbarPayloadsStore.db";
        DefaultRollbarStoreDbFile = new(Path.Combine(value.FullName, rollbarPayloadsStoreDb));
    }

    protected override void OnAccessTokenChanged(string accessToken)
    {
        IsConfigured = false;

        LocalAppDataDir = new(Path.Combine(_appInfoProvider.UserData?.FullName ?? Path.GetTempPath(),
            accessToken.GenerateMd5OfString()));

        LocalAppDataDir.Create();

        RollbarLocator.RollbarInstance.Configure(RollbarConfig.RollbarLoggerConfig);

        IsConfigured = true;
    }

    private static RollbarInfrastructureConfig Validate(RollbarInfrastructureConfig config)
    {
        var failedValidationRules = config.Validate();

        if (failedValidationRules.Count > 0)
            throw new AggregateException(AggregateErrors(failedValidationRules));

        return config;
    }

    private static IEnumerable<InvalidDataException> AggregateErrors(
        IReadOnlyCollection<ValidationResult> failedValidationRules) =>
        failedValidationRules.Select(validationResult => new InvalidDataException(validationResult.ToString(),
            validationResult.Details.Count > 0
                ? new AggregateException(validationResult.Details.Select(d => new InvalidDataException(d.ToString())))
                : null));

    private static string GetPersonId()
    {
        const string path = @"Software\Flakroup";
        const string keyName = "RollbarPersonID";

        //todo pass persistency service
        return FExPlatforms.RegistryService.GetOrAddRegistryKeyStringValue(path, keyName, GetNewPersonID);
    }

    private static string GetNewPersonID() => Guid.NewGuid().ToString();

    private static string GetFromFuncOrFallback(Func<string> func, string fallback = null)
    {
        var fromFunc = func?.Invoke();

        return fromFunc?.IsNotNullOrEmptyOrWhiteSpace() ?? false
            ? fromFunc
            : fallback;
    }

    private Person GetAppPerson() =>
        new(GetPersonId())
        {
            Email = PersonEmail?.Invoke(),
            UserName = GetFromFuncOrFallback(PersonUserName, DefaultID)
        };

    #region IDisposable
    public void Dispose() => _disposable?.Dispose();
    #endregion
}