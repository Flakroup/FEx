using FEx.Json.Abstractions;
using FEx.Json.Abstractions.Helpers;
using FEx.Json.Extensions;
using FEx.Json.Resolvers;
using Newtonsoft.Json;
using Shouldly;
using System.IO;
using System.Threading.Tasks;
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

    [Fact]
    public async Task TextWithoutAUtf8Form_ThrowsFExJsonException_FromTheTypeBasedStreamOverload()
    {
        var serializer = CreateSerializer(new());
        using var stream = new MemoryStream();

        // Newtonsoft writes an unpaired surrogate as is, and it cannot be encoded as UTF-8.
        await Should.ThrowAsync<FExJsonException>(() =>
            serializer.SerializeAsync(stream, "\uD800", typeof(string), TestContext.Current.CancellationToken));
    }
}
