using FEx.Agnostics.Abstractions.Extensions;
using FEx.AppSettings.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using FEx.Encryption;
using FEx.Json.Extensions;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace FEx.AppSettings.Abstractions;

public abstract class BaseUserSettings : SecureNotifyPropertyChanged, IBaseUserSettings
{
    private string _persistencePath;

    [JsonIgnore]
    public string PersistencePath
    {
        get => _persistencePath;
        private set => base.SetProperty(ref _persistencePath, value);
    }

    [JsonIgnore]
    public bool IsAsync { get; private set; }

    public virtual void Initialize(string persistencePath, (bool hasBeenReadFromFile, bool isAsync) tuple)
    {
        if (PersistencePath.IsNotNullOrEmptyString())
            FExCoreStatics.SynchronizedAccessService.RemoveLock(PersistencePath);

        PersistencePath = persistencePath;

        if (PersistencePath.IsNotNullOrEmptyString())
            Directory.CreateDirectory(Path.GetDirectoryName(PersistencePath)!);

        IsAsync = tuple.isAsync;

        if (!tuple.hasBeenReadFromFile)
            OnCreated();
    }

#pragma warning disable S2360
    public override bool SetProperty<TRet>(ref TRet backingField,
                                           TRet newValue,
                                           Action<TRet> onPropertyChanged = null,
                                           [CallerMemberName] string propertyName = null) =>
#pragma warning restore S2360
        base.SetProperty(ref backingField,
            newValue,
            _ =>
            {
                if (PersistencePath.IsNotNullOrEmptyString())
                {
                    if (IsAsync)
                        FExCoreStatics.AsyncHelper.FireTaskAndForget(SaveSettingsAsync);
                    else
                        SaveSettings();
                }

                onPropertyChanged?.Invoke(newValue);
            },
            propertyName);

    public void SaveSettings()
    {
        if (PersistencePath.IsNullOrEmptyString())
            return;

        FExCoreStatics.SynchronizedAccessService.Wait(PersistencePath);

        try
        {
            File.WriteAllText(PersistencePath, SerializedInstance());
        }
        finally
        {
            FExCoreStatics.SynchronizedAccessService.Release(PersistencePath);
        }
    }

    public async Task SaveSettingsAsync()
    {
        if (PersistencePath.IsNullOrEmptyString())
            return;

        await FExCoreStatics.SynchronizedAccessService.WaitAsync(PersistencePath);

        try
        {
#if NETSTANDARD2_0
            File.WriteAllText(PersistencePath, SerializedInstance());
#else
            await File.WriteAllTextAsync(PersistencePath, SerializedInstance());
#endif
        }
        finally
        {
            FExCoreStatics.SynchronizedAccessService.Release(PersistencePath);
        }
    }

    protected virtual string SerializedInstance() => this.ToJson(formatting: Formatting.Indented);

    protected virtual void OnCreated()
    {
    }
}

public abstract class BaseUserSettings<T> : BaseUserSettings where T : BaseUserSettings, new()
{
    public static T GetSettings(string persistencePath, bool isAsync)
    {
        var content = persistencePath.IsNotNullOrEmptyString() && File.Exists(persistencePath)
            ? File.ReadAllText(persistencePath)
            : null;

        var config = content.IsNotNullOrEmptyString()
            ? content.FromJson<T>()
            : new();

        config.Initialize(persistencePath, (content.IsNotNullOrEmptyString(), isAsync));

        return config;
    }

    public static T GetSettings() =>
        GetSettings(null, false);

    public static T GetSettings(string persistencePath) =>
        GetSettings(persistencePath, false);
}