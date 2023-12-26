using FEx.Json;
using Flurl.Http;
using Flurl.Http.Newtonsoft;
using System;

namespace FEx.Flurlx;

public class FlurlConfigurator : IFlurlConfigurator
{
    public void Configure() => FlurlHttp.Clients.WithDefaults(builder =>
    {
        builder.Settings.JsonSerializer = new NewtonsoftJsonSerializer(JsonExtensions.DefaultSettings);
        builder.Settings.Timeout = TimeSpan.FromMinutes(2);
    });
}