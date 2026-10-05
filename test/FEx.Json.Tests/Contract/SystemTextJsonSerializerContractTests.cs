using FEx.Json.Abstractions;
using FEx.Json.Abstractions.Helpers;
using FEx.Json.SystemTextJsonx;
using FEx.Json.SystemTextJsonx.Resolvers;
using Xunit;

namespace FEx.Json.Tests.Contract;

[Collection("FExServiceProvider")] // the transient DI test initializes the static provider
public sealed class SystemTextJsonSerializerContractTests : JsonSerializerContractTests
{
    protected override IFExJsonSerializer CreateSerializer(DIMeta diMeta) =>
        new FExSystemTextJsonSerializer(FExSystemTextJsonOptions.WithDIConstruction(
            FExSystemTextJsonOptions.CreateDefault(), new DIJsonTypeInfoModifier(diMeta)));
}
