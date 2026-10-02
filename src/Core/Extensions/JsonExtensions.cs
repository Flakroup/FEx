using FEx.Agnostics.Abstractions.Logging;
using FEx.Core.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FEx.Core.Extensions;

public static class JsonExtensions
{
    public static readonly JsonSerializerSettings Settings;

    static JsonExtensions()
    {
        // Work on a copy: the instance DefaultSettings returns can be shared process-wide, and the Handled error
        // handler below must not make every other JsonConvert call swallow errors.
        var defaults = JsonConvert.DefaultSettings?.Invoke();
        var settings = defaults is null ? new JsonSerializerSettings() : new JsonSerializerSettings(defaults);
        settings.NullValueHandling = NullValueHandling.Ignore;
        settings.MissingMemberHandling = MissingMemberHandling.Ignore;
        settings.PreserveReferencesHandling = PreserveReferencesHandling.None;
        settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        settings.ContractResolver = SafeContractResolver.Singleton;
        settings.Error = OnError;
        Settings = settings;
    }

    public static string SafeSerializeObject(this object initializeParameter) =>
        JsonConvert.SerializeObject(initializeParameter, Formatting.Indented, Settings);

    // Newtonsoft only suppresses the error and carries on when it is marked handled.
    private static void OnError(object? sender, ErrorEventArgs e)
    {
        FExStaticLogger.Error(e.ErrorContext.Error);
        e.ErrorContext.Handled = true;
    }
}