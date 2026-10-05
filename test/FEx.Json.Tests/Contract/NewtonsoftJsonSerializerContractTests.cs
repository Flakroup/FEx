using FEx.Json.Abstractions;
using FEx.Json.Abstractions.Helpers;
using FEx.Json.Extensions;
using FEx.Json.Resolvers;
using Newtonsoft.Json;
using Xunit;

namespace FEx.Json.Tests.Contract;

[Collection("FExServiceProvider")] // the transient DI test initializes the static provider
public sealed class NewtonsoftJsonSerializerContractTests : JsonSerializerContractTests
{
    protected override IFExJsonSerializer CreateSerializer(DIMeta diMeta) =>
        new FExNewtonsoftJsonSerializer(new JsonSerializerSettings(JsonExtensions.DefaultSettings!)
        {
            ContractResolver = new DIContractResolver(diMeta)
        });
}
