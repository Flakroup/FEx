using FEx.Encryption.Exceptions;
using FEx.Json.Extensions;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace FEx.Encryption.Tests;

/// <summary>
/// The defect these exist for: a failed decryption used to answer by writing null over the backing field,
/// so moving a settings file to another machine destroyed the protected data outright. The fix must not
/// trade that for a getter that throws either - these types are serialized whole on every property write,
/// and a throwing getter would let one unreadable field block saving all the others.
/// </summary>
public sealed class SecureNotifyPropertyChangedTests
{
    private const int FastIterations = FExStringCipher.MinIterations;

    private static readonly FExStringCipher _cipher = new("the owning passphrase", FastIterations);
    private static readonly FExStringCipher _stranger = new("an unrelated passphrase", FastIterations);

    [Fact]
    public void Property_RoundTrips()
    {
        var secrets = new Secrets(_cipher) { Secret = "hunter2" };

        secrets.Secret.ShouldBe("hunter2");
    }

    [Fact]
    public void Property_StoresCiphertextRatherThanPlaintext()
    {
        var secrets = new Secrets(_cipher) { Secret = "hunter2" };

        secrets.RawSecret.ShouldNotBeNull();
        secrets.RawSecret.ShouldNotContain("hunter2");
    }

    [Fact]
    public void Property_AcceptsAValueTheOldValidatorWouldHaveRejected()
    {
        // Tabs, newlines and symbols all failed the Unicode-category filter that used to guard this class.
        const string awkward = "line one\nline two\r\n\tindented ☺ ✓ 🎉";

        var secrets = new Secrets(_cipher) { Secret = awkward };

        secrets.Secret.ShouldBe(awkward);
    }

    [Fact]
    public void SettingNull_StoresNull()
    {
        var secrets = new Secrets(_cipher) { Secret = "hunter2" };

        secrets.Secret = null;

        secrets.RawSecret.ShouldBeNull();
        secrets.Secret.ShouldBeNull();
    }

    [Fact]
    public void UnreadableValue_IsLeftInPlaceAndReported()
    {
        var secrets = new Secrets(_cipher);
        var foreign = _stranger.Encrypt("written elsewhere");
        secrets.RawSecret = foreign;

        var read = secrets.Secret;

        read.ShouldBeNull();
        secrets.RawSecret.ShouldBe(foreign, "the stored ciphertext must survive a failed read");
        var failure = secrets.Failures.ShouldHaveSingleItem();
        failure.Property.ShouldBe(nameof(Secrets.Secret));
        failure.Exception.ShouldBeOfType<FExDecryptionException>();
    }

    [Fact]
    public void RejectedByValidation_IsLeftInPlaceAndReported()
    {
        var secrets = new Secrets(_cipher) { Secret = "hunter2" };
        var stored = secrets.RawSecret;
        secrets.RejectEverything = true;

        var read = secrets.Secret;

        read.ShouldBeNull();
        secrets.RawSecret.ShouldBe(stored, "failing validation must not erase the stored ciphertext either");
        secrets.Failures.ShouldHaveSingleItem().Exception.ShouldBeOfType<FExDecryptionException>();
    }

    [Fact]
    public void AnObjectWithAnUnreadableFieldStillSerializes()
    {
        // BaseUserSettings re-serializes on every property write, and a serializer walks every public
        // getter. If an unreadable field threw, saving any unrelated property would fail with it. Serializing
        // writes the stored ciphertext without decrypting it, so the unreadable value survives the save too.
        var foreign = _stranger.Encrypt("written elsewhere");
        var secrets = new Secrets(_cipher) { RawSecret = foreign };

        var json = Should.NotThrow(secrets.ToJson);

        json.ShouldContain(foreign);
    }

    [Fact]
    public void JsonProperty_RoundTrips()
    {
        var secrets = new Secrets(_cipher)
        {
            Data = new() { Name = "thing", Count = 7, Limits = new() { ["daily"] = 3 } }
        };

        secrets.Data.ShouldNotBeNull();
        secrets.Data!.Name.ShouldBe("thing");
        secrets.Data.Count.ShouldBe(7);
        secrets.Data.Limits.ShouldBe(new Dictionary<string, int> { ["daily"] = 3 });
    }

    [Fact]
    public void JsonProperty_WithAMalformedPayload_DegradesAndReports()
    {
        // Authentic bytes that are not the expected shape - what a model change looks like. Degrading beats
        // bringing the application down, but it must not be silent.
        var stored = _cipher.Encrypt("this is not json at all");
        var secrets = new Secrets(_cipher) { RawPayload = stored };

        secrets.Data.ShouldBeNull();
        secrets.RawPayload.ShouldBe(stored, "degrading to the default must not cost the stored value either");
        secrets.Failures.ShouldNotBeEmpty();
    }

    [Fact]
    public void JsonProperty_WithAMistypedMember_ReportsTheJsonPathButNotTheValue()
    {
        var secrets = new Secrets(_cipher) { RawPayload = _cipher.Encrypt("{\"Count\":\"hunter2\"}") };

        secrets.Data.ShouldBeNull();

        var failure = secrets.Failures.ShouldHaveSingleItem();
        failure.Exception.ShouldBeOfType<FExDecryptionException>();
        failure.Exception.Message.ShouldBe(
            $"The stored value of 'Data' is not a valid {nameof(Payload)} ({nameof(JsonReaderException)} at path 'Count').");
        failure.Exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void JsonProperty_WithAMistypedDictionaryValue_ReportsThePathWithoutTheKey()
    {
        // Newtonsoft leaves a plain key unquoted, so the raw path would be `Limits.password123`.
        var secrets = new Secrets(_cipher) { RawPayload = _cipher.Encrypt("{\"Limits\":{\"password123\":\"x\"}}") };

        secrets.Data.ShouldBeNull();

        secrets.Failures.ShouldHaveSingleItem().Exception.Message.ShouldBe(
            $"The stored value of 'Data' is not a valid {nameof(Payload)} ({nameof(JsonReaderException)} at path 'Limits[*]').");
    }

    [Fact]
    public void JsonProperty_FailingOutsideTheJsonReader_ReportsAnEmptyPath()
    {
        // A converter's own exception carries no JSON path; the report names its type and leaves the path empty.
        var secrets = new Secrets(_cipher) { RawUnreadable = _cipher.Encrypt("{}") };

        secrets.Unreadable.ShouldBeNull();

        secrets.Failures.ShouldHaveSingleItem().Exception.Message.ShouldBe(
            $"The stored value of 'Unreadable' is not a valid {nameof(UnreadablePayload)} ({nameof(InvalidOperationException)} at path '').");
    }

    private sealed class Payload
    {
        public string? Name { get; set; }

        public int Count { get; set; }

        public Dictionary<string, int>? Limits { get; set; }
    }

    [JsonConverter(typeof(ThrowingConverter))]
    private sealed class UnreadablePayload;

    private sealed class ThrowingConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader,
                                        Type objectType,
                                        object? existingValue,
                                        JsonSerializer serializer) =>
            throw new InvalidOperationException("Cannot read this payload.");

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
            throw new NotSupportedException();
    }

    private sealed class Secrets : SecureNotifyPropertyChanged
    {
        private readonly FExStringCipher _instanceCipher;
        private string? _secret;
        private string? _payload;

        internal Secrets(FExStringCipher cipher)
        {
            _instanceCipher = cipher;
        }

        internal List<(string? Property, Exception Exception)> Failures { get; } = [];

        internal bool RejectEverything { get; set; }

        internal string? RawSecret
        {
            get => _secret;
            set => _secret = value;
        }

        internal string? RawPayload
        {
            get => _payload;
            set => _payload = value;
        }

        internal string? RawUnreadable { get; set; }

        internal UnreadablePayload? Unreadable => DecryptFromJsonSource<UnreadablePayload>(RawUnreadable);

        public string? Secret
        {
            get => DecryptFromSource(_secret);
            set => EncryptSource(ref _secret, value);
        }

        public Payload? Data
        {
            get => DecryptFromJsonSource<Payload>(_payload);
            set => EncryptJsonSource(ref _payload, value);
        }

        protected override FExStringCipher Cipher => _instanceCipher;

        protected override bool IsValid(string? propertyName, string decryptedValue) => !RejectEverything;

        protected override void OnDecryptionFailed(string? propertyName, Exception exception) =>
            Failures.Add((propertyName, exception));
    }
}
