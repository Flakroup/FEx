using FEx.Json.Extensions;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
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
    public void JsonBackedProperty_WithoutAnyAttributes_IsWrittenAsCiphertext_AndRoundTrips()
    {
        var json = new JsonSettings { Data = new() { Name = Plaintext } }.ToJson();

        json.ShouldNotContain(Plaintext);
        JsonConvert.DeserializeObject<JsonSettings>(json)!.Data!.Name.ShouldBe(Plaintext);
    }

    [Fact]
    public void LegacyPlaintextJsonProperty_IsEncryptedOnLoad()
    {
        var read = JsonConvert.DeserializeObject<JsonSettings>($"{{\"Data\":{{\"Name\":\"{Plaintext}\"}}}}")!;

        read.Data!.Name.ShouldBe(Plaintext);
        read.ToJson().ShouldNotContain(Plaintext);
    }

    [Fact]
    public void EnvelopeShapedPlaintext_ThatFailsTheStructuralChecks_IsStillEncryptedOnLoad()
    {
        // Base64 of 85 bytes starting 0x01 - the version byte - but with an iteration count far outside the
        // accepted range: a legacy plaintext key, not a ciphertext, so it must not survive on disk verbatim.
        var bytes = new byte[85];

        for (var i = 1; i < bytes.Length; i++)
            bytes[i] = (byte)(i * 7);

        bytes[0] = 1;
        var legacy = Convert.ToBase64String(bytes);

        var read = JsonConvert.DeserializeObject<PlainSettings>($"{{\"Secret\":\"{legacy}\"}}")!;

        read.Secret.ShouldBe(legacy);
        read.ToJson().ShouldNotContain(legacy);
    }

    [Fact]
    public void HandledSerializerError_NeverLetsALaterPropertyOutInTheClear()
    {
        var settings = new ThrowingFirstSettings { Secret = Plaintext };
        JsonSerializerSettings tolerant = new() { Error = (_, e) => e.ErrorContext.Handled = true };

        var json = JsonConvert.SerializeObject(settings, tolerant);

        json.ShouldNotContain(Plaintext);
    }

    [Fact]
    public void HelperGetter_WithACtorAssignedField_IsWrittenAsCiphertext_AndRoundTripsUnchanged()
    {
        // [CallerMemberName] names the helper, not the property, and no setter ever ran - the converter must
        // still find the ciphertext rather than fall back to the getter's plaintext.
        var settings = new HelperSettings();

        var json = settings.ToJson();

        json.ShouldNotContain(Plaintext);
        json.ShouldContain(settings.RawSecret!);
        var read = JsonConvert.DeserializeObject<HelperSettings>(json)!;
        read.Secret.ShouldBe(Plaintext);
        read.RawSecret.ShouldBe(settings.RawSecret, "the stored ciphertext must be kept, not encrypted again");
    }

    [Fact]
    public void GetterThatDecryptsTwoValues_FailsClosed()
    {
        var settings = new TwoSecretsSettings();
        JsonSerializerSettings tolerant = new() { Error = (_, e) => e.ErrorContext.Handled = true };

        Should.Throw<InvalidOperationException>(() => JsonConvert.SerializeObject(settings));
        var json = JsonConvert.SerializeObject(settings, tolerant);

        json.ShouldNotContain(Plaintext);
        json.ShouldContain("kept");
    }

    [Fact]
    public void HandledSerializerError_OnWrite_SkipsOnlyTheFailingMember()
    {
        var settings = new ThrowingFirstSettings { Secret = Plaintext, Visible = "kept" };
        JsonSerializerSettings tolerant = new() { Error = (_, e) => e.ErrorContext.Handled = true };

        var json = JsonConvert.SerializeObject(settings, tolerant);

        json.ShouldNotContain("Boom");
        json.ShouldContain("\"Visible\":\"kept\"");
        json.ShouldContain(settings.RawSecret!);
    }

    [Fact]
    public void HandledSerializerError_OnRead_SkipsOnlyTheFailingMember()
    {
        var ciphertext = _cipher.Encrypt(Plaintext);
        var json = $"{{\"Fragile\":\"x\",\"Secret\":\"{ciphertext}\",\"Visible\":\"kept\"}}";
        JsonSerializerSettings tolerant = new() { Error = (_, e) => e.ErrorContext.Handled = true };

        var read = JsonConvert.DeserializeObject<FragileSettings>(json, tolerant);

        read.ShouldNotBeNull();
        read.Visible.ShouldBe("kept");
        read.Secret.ShouldBe(Plaintext);
    }

    [Fact]
    public async Task ConcurrentSerializationOfOneInstance_NeverWritesPlaintext()
    {
        var ct = TestContext.Current.CancellationToken;
        using ManualResetEventSlim firstIsInside = new();
        using ManualResetEventSlim release = new();
        var settings = new SlowSettings(firstIsInside, release) { Secret = Plaintext };

        var first = Task.Run(settings.ToJson, ct);
        firstIsInside.Wait(TimeSpan.FromSeconds(10), ct).ShouldBeTrue();
        var second = settings.ToJson();
        release.Set();

        (await first).ShouldNotContain(Plaintext);
        second.ShouldNotContain(Plaintext);
    }

    [Fact]
    public void ThrowingSerializationCallback_LeavesGettersAndTheNextSaveIntact()
    {
        var settings = new ThrowingCallbackSettings { Secret = Plaintext, Throw = true };

        Should.Throw<Exception>(settings.ToJson);
        settings.Secret.ShouldBe(Plaintext);

        settings.Throw = false;
        settings.ToJson().ShouldNotContain(Plaintext);
    }

    [Fact]
    public void PopulateObject_WhichBypassesTheConverter_DoesNotEncryptTwice()
    {
        var json = new PlainSettings { Secret = Plaintext }.ToJson();
        PlainSettings target = new();

        JsonConvert.PopulateObject(json, target);

        target.Secret.ShouldBe(Plaintext);
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

    public sealed class JsonSettings : SecureNotifyPropertyChanged
    {
        private string? _payload;

        public Payload? Data
        {
            get => DecryptFromJsonSource<Payload>(_payload);
            set => EncryptJsonSource(ref _payload, value);
        }

        protected override FExStringCipher Cipher => _cipher;
    }

    /// <summary>A member whose getter throws, declared before the secret, as in the reviewed leak.</summary>
    public sealed class ThrowingFirstSettings : SecureNotifyPropertyChanged
    {
        private string? _secret;

        public string Boom => throw new InvalidOperationException("boom");

        public string? Visible { get; set; }

        internal string? RawSecret => _secret;

        public string? Secret
        {
            get => DecryptFromSource(_secret);
            set => EncryptSource(ref _secret, value);
        }

        protected override FExStringCipher Cipher => _cipher;
    }

    /// <summary>Its first member blocks the first reader until another serialization has finished.</summary>
    public sealed class SlowSettings : SecureNotifyPropertyChanged
    {
        private readonly ManualResetEventSlim _firstIsInside;
        private readonly ManualResetEventSlim _release;
        private int _reads;
        private string? _secret;

        public SlowSettings(ManualResetEventSlim firstIsInside, ManualResetEventSlim release)
        {
            _firstIsInside = firstIsInside;
            _release = release;
        }

        public string Slow
        {
            get
            {
                if (Interlocked.Increment(ref _reads) == 1)
                {
                    _firstIsInside.Set();
                    _release.Wait(TimeSpan.FromSeconds(10));
                }

                return "s";
            }
        }

        public string? Secret
        {
            get => DecryptFromSource(_secret);
            set => EncryptSource(ref _secret, value);
        }

        protected override FExStringCipher Cipher => _cipher;
    }

    public sealed class ThrowingCallbackSettings : SecureNotifyPropertyChanged
    {
        private string? _secret;

        [JsonIgnore]
        public bool Throw { get; set; }

        public string? Secret
        {
            get => DecryptFromSource(_secret);
            set => EncryptSource(ref _secret, value);
        }

        protected override FExStringCipher Cipher => _cipher;

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (Throw)
                throw new InvalidOperationException("callback");
        }
    }

    /// <summary>Getter and setter go through helpers; the field is assigned in the constructor.</summary>
    public sealed class HelperSettings : SecureNotifyPropertyChanged
    {
        private string? _secret = _cipher.Encrypt(Plaintext);

        public string? Secret
        {
            get => Read(_secret);
            set => Write(ref _secret, value);
        }

        internal string? RawSecret => _secret;

        protected override FExStringCipher Cipher => _cipher;

        private string? Read(string? source) => DecryptFromSource(source);

        private void Write(ref string? field, string? value) => EncryptSource(ref field, value);
    }

    public sealed class TwoSecretsSettings : SecureNotifyPropertyChanged
    {
        private readonly string _first = _cipher.Encrypt(Plaintext);
        private readonly string _second = _cipher.Encrypt("other");

        public string Visible => "kept";

        public string Combined => DecryptFromSource(_first) + DecryptFromSource(_second);

        protected override FExStringCipher Cipher => _cipher;
    }

    /// <summary>Its first member's setter throws on read.</summary>
    public sealed class FragileSettings : SecureNotifyPropertyChanged
    {
        private string? _secret;

        public string? Fragile
        {
            get => null;
            set => throw new InvalidOperationException("fragile");
        }

        public string? Secret
        {
            get => DecryptFromSource(_secret);
            set => EncryptSource(ref _secret, value);
        }

        public string? Visible { get; set; }

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
