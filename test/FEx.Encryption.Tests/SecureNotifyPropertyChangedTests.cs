using FEx.Encryption.Exceptions;
using FEx.Json.Extensions;
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
        // getter. If an unreadable field threw, saving any unrelated property would fail with it.
        var secrets = new Secrets(_cipher) { RawSecret = _stranger.Encrypt("written elsewhere") };

        var json = Should.NotThrow(secrets.ToJson);

        json.ShouldNotBeNullOrEmpty();
        secrets.Failures.ShouldNotBeEmpty();
    }

    [Fact]
    public void JsonProperty_RoundTrips()
    {
        var secrets = new Secrets(_cipher) { Data = new() { Name = "thing", Count = 7 } };

        secrets.Data.ShouldNotBeNull();
        secrets.Data!.Name.ShouldBe("thing");
        secrets.Data.Count.ShouldBe(7);
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

    private sealed class Payload
    {
        public string? Name { get; set; }

        public int Count { get; set; }
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
