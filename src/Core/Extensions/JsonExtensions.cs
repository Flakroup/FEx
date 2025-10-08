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
        JsonSerializerSettings settings = JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings();
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

    private static void OnError(object sender, ErrorEventArgs e) => FExStaticLogger.Error(e.ErrorContext.Error);
}