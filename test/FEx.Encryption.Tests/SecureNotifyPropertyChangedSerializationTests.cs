using FEx.Json.Extensions;
using Newtonsoft.Json;
using Shouldly;
using System;
using Xunit;

namespace FEx.Encryption.Tests;

/// <summary>
/// The defect these exist for: a property getter returns plaintext, so a consumer who declared an encrypted
/// property the obvious way - no serialization attributes - got the secret written to the settings file in
/// the clear. Serializing must persist ciphertext by default, and reading it back must not encrypt it twice.
/// </summary>
public sealed class SecureNotifyPropertyChangedSerializationTests
{
    private const string Plaintext = "hunter2";

    private static readonly FExStringCipher _cipher = new("the owning passphrase", FExStringCipher.MinIterations);
    private static readonly FExStringCipher _stranger = new("an unrelated passphrase", FExStringCipher.MinIterations);

    [Fact]
    public void PlainProperty_IsSerializedAsItsCiphertext()
    {
        var settings = new PlainSettings { Secret = Plaintext };

        var json = settings.ToJson();

        json.ShouldNotContain(Plaintext);
        json.ShouldContain(settings.RawSecret!);
    }

    [Fact]
    public void PlainProperty_RoundTripsThroughJson()
    {
        var json = new PlainSettings { Secret = Plaintext, Visible = "kept" }.ToJson();

        var read = JsonConvert.DeserializeObject<PlainSettings>(json)!;

        read.Secret.ShouldBe(Plaintext);
        read.Visible.ShouldBe("kept");
    }

    [Fact]
    public void Getter_ReturnsPlaintextAgain_OnceSerializationIsOver()
    {
        var settings = new PlainSettings { Secret = Plaintext };

        _ = settings.ToJson();

        settings.Secret.ShouldBe(Plaintext);
    }

    [Fact]
    public void LegacyPlaintextFile_IsEncryptedOnLoad_SoTheNextSaveMigratesIt()
    {
        var read = JsonConvert.DeserializeObject<PlainSettings>($"{{\"Secret\":\"{Plaintext}\"}}")!;

        read.Secret.ShouldBe(Plaintext);
        read.RawSecret.ShouldNotBe(Plaintext);
        read.ToJson().ShouldNotContain(Plaintext);
    }

    [Fact]
    public void ForeignCiphertext_IsKeptAsIs_NotEncryptedASecondTime()
    {
        var foreign = _stranger.Encrypt("written elsewhere");

        var read = JsonConvert.DeserializeObject<PlainSettings>($"{{\"Secret\":\"{foreign}\"}}")!;

        read.RawSecret.ShouldBe(foreign);
    }

    [Fact]
    public void JsonBackedProperty_WithoutTheFieldPattern_RefusesToSerialize_AndLeavesTheGettersWorking()
    {
        var settings = new LeakyJsonSettings { Data = new() { Name = Plaintext } };

        var ex = Should.Throw<JsonSerializationException>(() => settings.ToJson());

        ex.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldContain(nameof(LeakyJsonSettings.Data));
        settings.Data!.Name.ShouldBe(Plaintext, "a failed save must not leave the getters returning ciphertext");
    }

    [Fact]
    public void JsonBackedProperty_WithTheFieldPattern_RoundTripsWithoutPlaintext()
    {
        var json = new FieldPatternSettings { Data = new() { Name = Plaintext } }.ToJson();

        json.ShouldNotContain(Plaintext);
        JsonConvert.DeserializeObject<FieldPatternSettings>(json)!.Data!.Name.ShouldBe(Plaintext);
    }

    public sealed class Payload
    {
        public string? Name { get; set; }
    }

    /// <summary>The obvious declaration - no attributes at all.</summary>
    public sealed class PlainSettings : SecureNotifyPropertyChanged
    {
        private string? _secret;

        public string? Visible { get; set; }

        public string? Secret
        {
            get => DecryptFromSource(_secret);
            set => EncryptSource(ref _secret, value);
        }

        internal string? RawSecret => _secret;

        protected override FExStringCipher Cipher => _cipher;
    }

    public sealed class LeakyJsonSettings : SecureNotifyPropertyChanged
    {
        private string? _payload;

        public Payload? Data
        {
            get => DecryptFromJsonSource<Payload>(_payload);
            set => EncryptJsonSource(ref _payload, value);
        }

        protected override FExStringCipher Cipher => _cipher;
    }

    public sealed class FieldPatternSettings : SecureNotifyPropertyChanged
    {
        [JsonProperty("payload")]
        private string? _payload;

        [JsonIgnore]
        public Payload? Data
        {
            get => DecryptFromJsonSource<Payload>(_payload);
            set => EncryptJsonSource(ref _payload, value);
        }

        protected override FExStringCipher Cipher => _cipher;
    }
}
