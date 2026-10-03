using FEx.Core.Extensions;
using Newtonsoft.Json;
using Shouldly;
using System;
using Xunit;

namespace FEx.Core.Tests.Extensions;

// The error handler logs through FExStaticLogger, which other tests swap.
[Collection(StaticStateCollection.Name)]
public sealed class JsonExtensionsTests
{
    [Fact]
    public void SafeSerializeObject_SkipsThrowingMember_WithoutMakingDefaultSettingsSwallowErrors()
    {
        // JsonExtensions' static constructor reads DefaultSettings once, on first use. Keeping everything in this
        // single test, the only one touching it, guarantees DefaultSettings is installed before that happens.
        var original = JsonConvert.DefaultSettings;
        var shared = new JsonSerializerSettings();

        try
        {
            JsonConvert.DefaultSettings = () => shared;

            string json = null!;

            Should.NotThrow(() => json = new Faulty().SafeSerializeObject());

            json.ShouldContain(nameof(Faulty.Name));
            json.ShouldContain("ok");
            json.ShouldNotContain(nameof(Faulty.Boom));

            shared.Error.ShouldBeNull();
            Should.Throw<JsonReaderException>(() => JsonConvert.DeserializeObject<Dto>($"{{\"{nameof(Dto.Id)}\":\"not-an-int\"}}"));
        }
        finally
        {
            JsonConvert.DefaultSettings = original;
        }
    }

    private sealed class Dto
    {
        public int Id { get; set; }
    }

    private sealed class Faulty
    {
        public string Name { get; } = "ok";

        public string Boom => throw new InvalidOperationException("cannot read");
    }
}
