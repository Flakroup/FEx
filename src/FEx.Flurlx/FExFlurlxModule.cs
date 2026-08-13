using Flurl.Http.Configuration;
using StrongInject;
using System.Text.Json;

namespace FEx.Flurlx;

/// <summary>
/// Default FEx.Flurlx module: serializer-agnostic core registrations plus a
/// System.Text.Json serializer (<see cref="DefaultJsonSerializer"/> with
/// <see cref="JsonSerializerDefaults.Web"/>). Zero Newtonsoft dependency.
/// For Newtonsoft, register the FEx.Flurlx.Newtonsoft module instead of this one.
/// </summary>
[RegisterModule(typeof(FExFlurlxBaseModule))]
public class FExFlurlxModule
{
    [Factory(Scope.SingleInstance)]
    public static ISerializer JsonSerializerFactory() =>
        new DefaultJsonSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
}
