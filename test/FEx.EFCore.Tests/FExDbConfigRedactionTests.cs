using FEx.EFCore.Configuration;
using Newtonsoft.Json;
using Shouldly;
using StjSerializer = System.Text.Json.JsonSerializer;
using Xunit;

namespace FEx.EFCore.Tests;

/// <summary>
/// A database password must never surface through the config object's textual renderings - a logged
/// record or a serialized snapshot - while staying settable and readable through the property itself.
/// </summary>
public sealed class FExDbConfigRedactionTests
{
    private const string Secret = "hunter2-Pa55";

    private static FExDbConfig Config() =>
        new()
        {
            SqlInstance = "db.local",
            SqlDbName = "app",
            Username = "svc",
            Password = Secret
        };

    [Fact]
    public void ToString_RedactsThePassword_ButKeepsTheOtherMembers()
    {
        var text = Config().ToString();

        text.ShouldNotContain(Secret);
        text.ShouldContain("Password = ***");
        text.ShouldContain("SqlInstance = db.local");
        text.ShouldContain("Username = svc");
    }

    [Fact]
    public void ToString_OfADerivedRecord_StillRedactsThePassword() =>
        new DerivedConfig { Password = Secret, Extra = "x" }.ToString().ShouldNotContain(Secret);

    [Fact]
    public void Password_StaysReadableThroughTheProperty() => Config().Password.ShouldBe(Secret);

    [Fact]
    public void SystemTextJson_DoesNotWriteThePassword_ButStillReadsIt()
    {
        var json = StjSerializer.Serialize(Config());

        json.ShouldNotContain(Secret);
        json.ShouldContain("db.local");
        StjSerializer.Deserialize<FExDbConfig>("{\"Password\":\"" + Secret + "\"}")!
            .Password.ShouldBe(Secret);
    }

    [Fact]
    public void NewtonsoftJson_DoesNotWriteThePassword_ButStillReadsIt()
    {
        var json = JsonConvert.SerializeObject(Config());

        json.ShouldNotContain(Secret);
        json.ShouldContain("db.local");
        JsonConvert.DeserializeObject<FExDbConfig>("{\"Password\":\"" + Secret + "\"}")!.Password.ShouldBe(Secret);
    }

    private sealed record DerivedConfig : FExDbConfig
    {
        public string? Extra { get; init; }
    }
}
