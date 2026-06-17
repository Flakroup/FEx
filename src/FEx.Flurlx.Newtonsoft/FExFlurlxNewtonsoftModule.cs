using FEx.Json.Extensions;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using StrongInject;

namespace FEx.Flurlx.Newtonsoft;

/// <summary>
/// Opt-in FEx.Flurlx module that swaps the default System.Text.Json serializer for
/// Newtonsoft.Json (with <see cref="JsonExtensions.DefaultSettings"/>, matching the
/// legacy FEx.Flurlx behavior). Register this <b>instead of</b> <see cref="FExFlurlxModule"/>.
/// </summary>
[RegisterModule(typeof(FExFlurlxBaseModule))]
public class FExFlurlxNewtonsoftModule
{
    [Factory(Scope.SingleInstance)]
    public static ISerializer JsonSerializerFactory() =>
        new NewtonsoftJsonSerializer(JsonExtensions.DefaultSettings);
}
